using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Tech_Store.Models;

namespace Tech_Store.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RedisService> _logger;
        private static readonly Regex InvalidKeywordCharacters = new(@"[^\p{L}\p{N}\s\-\+]", RegexOptions.Compiled);
        private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);

        public RedisService(IConnectionMultiplexer redis, ApplicationDbContext context, ILogger<RedisService> logger)
        {
            _db = redis.GetDatabase();
            _context = context;
            _logger = logger;
        }

        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            await TryRedisAsync(() => _db.StringSetAsync(key, value, expiry));
        }

        public async Task<string?> GetAsync(string key)
        {
            return await TryRedisAsync(async () => (string?)await _db.StringGetAsync(key), default(string?));
        }

        public async Task<long> IncrementAsync(string key, TimeSpan? expiry = null)
        {
            var value = await TryRedisAsync(() => _db.StringIncrementAsync(key), 0L);
            if (expiry.HasValue)
            {
                await TryRedisAsync(() => _db.KeyExpireAsync(key, expiry));
            }

            return value;
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            return await TryRedisAsync(() => _db.KeyTimeToLiveAsync(key), null as TimeSpan?);
        }

        public async Task DeleteAsync(string key)
        {
            await TryRedisAsync(() => _db.KeyDeleteAsync(key));
        }

        public async Task IncrementHotKeywordAsync(string keyword, string categorySlug)
        {
            await IncrementHotKeywordAsync(keyword, new[] { categorySlug });
        }

        public async Task IncrementHotKeywordAsync(string keyword, IEnumerable<string> categorySlugs, double weight = 1)
        {
            var normalizedKeyword = NormalizeHotKeyword(keyword);
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                return;
            }

            var slugs = categorySlugs
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (slugs.Count == 0)
            {
                slugs.Add("all");
            }

            foreach (var slug in slugs)
            {
                await TryRedisAsync(() => _db.SortedSetIncrementAsync($"hot_keywords:{slug}", normalizedKeyword, weight));
            }

            await TryRedisAsync(() => _db.SortedSetIncrementAsync("hot_keywords:all", normalizedKeyword, weight));
        }

        public async Task<Dictionary<string, List<string>>> GetTopSearchKeywords(List<Category> categories, int count = 10)
        {
            var result = new Dictionary<string, List<string>>();

            foreach (var category in categories.Where(x => !string.IsNullOrWhiteSpace(x.EngTitle)))
            {
                result[category.EngTitle] = await GetTopSearchKeywordsByCategory(category.EngTitle, count);
            }

            return result;
        }

        public async Task<List<string>> GetTopSearchKeywordsByCategory(string categorySlug, int count = 10)
        {
            if (string.IsNullOrWhiteSpace(categorySlug))
            {
                return new List<string>();
            }

            var normalizedSlug = categorySlug.Trim().ToLowerInvariant();
            var redisKeywords = await TryRedisAsync(
                () => _db.SortedSetRangeByRankAsync(
                    $"hot_keywords:{normalizedSlug}",
                    0,
                    count - 1,
                    StackExchange.Redis.Order.Descending),
                Array.Empty<RedisValue>());

            var keywords = redisKeywords
                .Select(x => x.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (keywords.Count >= count)
            {
                return keywords;
            }

            var fallbackKeywords = await BuildFallbackKeywordsAsync(normalizedSlug, count);
            foreach (var fallbackKeyword in fallbackKeywords)
            {
                if (keywords.Count >= count)
                {
                    break;
                }

                if (!keywords.Contains(fallbackKeyword))
                {
                    keywords.Add(fallbackKeyword);
                }
            }

            return keywords;
        }

        public async Task EnsureHotKeywordsSeededAsync(IEnumerable<Category> categories, int minimumCountPerCategory = 10)
        {
            await Task.CompletedTask;
        }

        public string? NormalizeHotKeyword(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return null;
            }

            var normalized = InvalidKeywordCharacters.Replace(keyword, " ");
            normalized = MultiWhitespace.Replace(normalized, " ").Trim().ToLowerInvariant();

            if (normalized.Length < 2 || normalized.Length > 80)
            {
                return null;
            }

            if (normalized.All(char.IsDigit))
            {
                return null;
            }

            return normalized;
        }

        private async Task<List<string>> BuildFallbackKeywordsAsync(string categorySlug, int minimumCount)
        {
            var seeds = new List<string>();

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EngTitle == categorySlug);

            if (category != null)
            {
                AddKeyword(seeds, category.Name);
                AddKeyword(seeds, category.EngTitle?.Replace("-", " "));

                var brandKeywords = await _context.Brands
                    .AsNoTracking()
                    .Where(b => b.CategoryId == category.CategoryId ||
                                b.BrandCategories.Any(bc => bc.CategoryId == category.CategoryId))
                    .OrderBy(b => b.SortOrder ?? 0)
                    .Select(b => b.Name)
                    .Distinct()
                    .Take(8)
                    .ToListAsync();

                foreach (var brandKeyword in brandKeywords)
                {
                    var normalized = NormalizeHotKeyword(brandKeyword);
                    if (!string.IsNullOrWhiteSpace(normalized) && !seeds.Contains(normalized))
                    {
                        seeds.Add(normalized);
                    }
                }

                var productKeywords = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.CategoryId == category.CategoryId && p.Visible == true && p.Status != "outstock")
                    .OrderByDescending(p => p.OrderItems.Count)
                    .ThenByDescending(p => p.ProductId)
                    .Select(p => p.Name)
                    .Take(12)
                    .ToListAsync();

                foreach (var productKeyword in productKeywords)
                {
                    var normalized = NormalizeHotKeyword(CompactProductKeyword(productKeyword));
                    if (!string.IsNullOrWhiteSpace(normalized) && !seeds.Contains(normalized))
                    {
                        seeds.Add(normalized);
                    }
                }
            }

            return seeds.Take(Math.Max(minimumCount, 12)).ToList();
        }

        private void AddKeyword(List<string> keywords, string? rawKeyword)
        {
            var normalized = NormalizeHotKeyword(rawKeyword);
            if (!string.IsNullOrWhiteSpace(normalized) && !keywords.Contains(normalized))
            {
                keywords.Add(normalized);
            }
        }

        private static string CompactProductKeyword(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return string.Empty;
            }

            var compact = productName.Split('|', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            compact = compact.Replace("Chính hãng Apple Việt Nam", "", StringComparison.OrdinalIgnoreCase).Trim();
            compact = compact.Replace("Chính hãng VN/A", "", StringComparison.OrdinalIgnoreCase).Trim();
            compact = compact.Replace("kèm bút, bàn phím", "", StringComparison.OrdinalIgnoreCase).Trim();

            return compact.Length <= 48 ? compact : compact[..48].Trim();
        }

        #region Recommendation

        // Lưu thông tin xem cuối sản phẩm của user để làm recommend personality
        public async Task TrackUserWatchedProductAsync(int userId, string product_sys_Id, double weight = 1, int maxItems = 10)
        {
            var listKey = $"user:{userId}:latest_watched";
            var weightKey = $"user:{userId}:watched_weights";
            var productIdStr = product_sys_Id;

            await TryRedisAsync(() => _db.ListRemoveAsync(listKey, productIdStr));
            await TryRedisAsync(() => _db.ListLeftPushAsync(listKey, productIdStr));

            await TryRedisAsync(() => _db.ListTrimAsync(listKey, 0, maxItems - 1));

            await TryRedisAsync(() => _db.SortedSetIncrementAsync(weightKey, productIdStr, weight));

            await TryRedisAsync(() => _db.KeyExpireAsync(listKey, TimeSpan.FromDays(30)));
            await TryRedisAsync(() => _db.KeyExpireAsync(weightKey, TimeSpan.FromDays(30)));
        }

        public async Task TrackGuestWatchedProductAsync(string guestId, string product_sys_Id, double weight = 1, int maxItems = 10)
        {
            var listKey = $"guest:{guestId}:latest_watched";
            var weightKey = $"guest:{guestId}:watched_weights";
            var productIdStr = product_sys_Id;
            await TryRedisAsync(() => _db.ListRemoveAsync(listKey, productIdStr));
            await TryRedisAsync(() => _db.ListLeftPushAsync(listKey, productIdStr));
            await TryRedisAsync(() => _db.ListTrimAsync(listKey, 0, maxItems - 1));
            await TryRedisAsync(() => _db.SortedSetIncrementAsync(weightKey, productIdStr, weight));
            await TryRedisAsync(() => _db.KeyExpireAsync(listKey, TimeSpan.FromDays(7)));
            await TryRedisAsync(() => _db.KeyExpireAsync(weightKey, TimeSpan.FromDays(7)));
        }

        public async Task<List<string>> GetUserLatestWatchedAsync(int userId, int count = 10)
        {
            var listKey = $"user:{userId}:latest_watched";
            var values = await TryRedisAsync(() => _db.ListRangeAsync(listKey, 0, count - 1), Array.Empty<RedisValue>());

            return values.Select(x => x.ToString()).ToList();
        }

        public async Task MergeGuestToUserHistory(string guestId, int userId)
        {
            if (string.IsNullOrEmpty(guestId)) return;

            var guestListKey = $"guest:{guestId}:latest_watched";
            var userListKey = $"user:{userId}:latest_watched";

            var guestListData = await TryRedisAsync(() => _db.ListRangeAsync(guestListKey), Array.Empty<RedisValue>());
            if (guestListData.Length > 0)
            {
                foreach (var item in guestListData.Reverse())
                {
                    await TryRedisAsync(() => _db.ListRemoveAsync(userListKey, item));
                    await TryRedisAsync(() => _db.ListLeftPushAsync(userListKey, item));
                }
                await TryRedisAsync(() => _db.ListTrimAsync(userListKey, 0, 9));
                await TryRedisAsync(() => _db.KeyExpireAsync(userListKey, TimeSpan.FromDays(30)));
                await TryRedisAsync(() => _db.KeyDeleteAsync(guestListKey));
            }

            var guestWeightKey = $"guest:{guestId}:watched_weights";
            var userWeightKey = $"user:{userId}:watched_weights";

            var guestWeightData = await TryRedisAsync(
                () => _db.SortedSetRangeByRankWithScoresAsync(guestWeightKey),
                Array.Empty<SortedSetEntry>());
            if (guestWeightData.Length > 0)
            {
                foreach (var item in guestWeightData)
                {
                    await TryRedisAsync(() => _db.SortedSetIncrementAsync(userWeightKey, item.Element, item.Score));
                }
                await TryRedisAsync(() => _db.KeyExpireAsync(userWeightKey, TimeSpan.FromDays(30)));
                await TryRedisAsync(() => _db.KeyDeleteAsync(guestWeightKey));
            }
        }

        #endregion

        private async Task TryRedisAsync(Func<Task> action, [CallerMemberName] string operation = "")
        {
            try
            {
                await action();
            }
            catch (Exception ex) when (IsRedisUnavailable(ex))
            {
                _logger.LogWarning(ex, "Redis operation {Operation} failed. Falling back without Redis.", operation);
            }
        }

        private async Task<T> TryRedisAsync<T>(Func<Task<T>> action, T fallback, [CallerMemberName] string operation = "")
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (IsRedisUnavailable(ex))
            {
                _logger.LogWarning(ex, "Redis operation {Operation} failed. Using fallback result.", operation);
                return fallback;
            }
        }

        private static bool IsRedisUnavailable(Exception ex)
        {
            return ex is RedisConnectionException ||
                   ex is RedisTimeoutException ||
                   ex is TimeoutException;
        }
    }
}
