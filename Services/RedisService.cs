using StackExchange.Redis;
using System.Runtime.CompilerServices;
using Tech_Store.Models;

namespace Tech_Store.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;
        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SetAsync(string key, string value)
        {
            await _db.StringSetAsync(key, value);
        }

        public async Task<string?> GetAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }
        //Ghi dòng mới cho hot keyword
        public async Task IncrementHotKeywordAsync(string keyword,string categorySlug)
        {
            await _db.SortedSetIncrementAsync($"hot_keywords:{categorySlug}", keyword, 1);
        }
        //Lấy list hot search cho từng Category
        public async Task<Dictionary<string, List<string>>> GetTopSearchKeywords(List<Category> categories, int count = 10)
        {
            var result = new Dictionary<string, List<string>>();

            foreach (var category in categories)
            {
                var redisKey = $"hot_keywords:{category.EngTitle}";
                var keywords = await _db.SortedSetRangeByRankAsync(redisKey, 0, count - 1, StackExchange.Redis.Order.Descending);
                result[category.EngTitle] = keywords.Select(x => x.ToString()).ToList();
            }

            return result;
        }
        //Lấy hot search theo mã Category
        public async Task<List<string>> GetTopSearchKeywordsByCategory(string categorySlug, int count = 10)
        {
            var redisKey = $"hot_keywords:{categorySlug}";
            var keywords = await _db.SortedSetRangeByRankAsync(redisKey, 0, count - 1, StackExchange.Redis.Order.Descending);
            return keywords.Select(x => x.ToString()).ToList();
        }

        #region Recommendation

        // Lưu thông tin xem cuối sản phẩm của user để làm recommend personality
        public async Task TrackUserWatchedProductAsync(int userId, string product_sys_Id, double weight = 1 , int maxItems = 10)
        {
            var listKey = $"user:{userId}:latest_watched";
            var weightKey = $"user:{userId}:watched_weights";
            var productIdStr = product_sys_Id;

            await _db.ListRemoveAsync(listKey, productIdStr);
            await _db.ListLeftPushAsync(listKey, productIdStr);

            await _db.ListTrimAsync(listKey, 0, maxItems - 1);

            await _db.SortedSetIncrementAsync(weightKey, productIdStr, weight);

            await _db.KeyExpireAsync(listKey, TimeSpan.FromDays(30));
            await _db.KeyExpireAsync(weightKey, TimeSpan.FromDays(30));

        }

        public async Task TrackGuestWatchedProductAsync(string guestId, string product_sys_Id, double weight = 1, int maxItems = 10)
        {
            var listKey = $"guest:{guestId}:latest_watched";
            var weightKey = $"guest:{guestId}:watched_weights";
            var productIdStr = product_sys_Id;
            await _db.ListRemoveAsync(listKey, productIdStr);
            await _db.ListLeftPushAsync(listKey, productIdStr);
            await _db.ListTrimAsync(listKey, 0, maxItems - 1);
            await _db.SortedSetIncrementAsync(weightKey, productIdStr, weight);
            await _db.KeyExpireAsync(listKey, TimeSpan.FromDays(7));
            await _db.KeyExpireAsync(weightKey, TimeSpan.FromDays(7));
        }
        public async Task<List<string>> GetUserLatestWatchedAsync(int userId, int count = 10)
        {
            var listKey = $"user:{userId}:latest_watched";
            var values = await _db.ListRangeAsync(listKey, 0, count - 1);

            return values.Select(x => x.ToString()).ToList();
        }

        public async Task MergeGuestToUserHistory(string guestId, int userId)
        {
            if (string.IsNullOrEmpty(guestId)) return;

            // --- 1. Merge "latest_watched" (Kiểu LIST) ---
            var guestListKey = $"guest:{guestId}:latest_watched";
            var userListKey = $"user:{userId}:latest_watched";

            var guestListData = await _db.ListRangeAsync(guestListKey);
            if (guestListData.Length > 0)
            {
                foreach (var item in guestListData.Reverse()) 
                {
                    await _db.ListRemoveAsync(userListKey, item);
                    await _db.ListLeftPushAsync(userListKey, item);
                }
                await _db.ListTrimAsync(userListKey, 0, 9); // Giữ lại 10 cái mới nhất
                await _db.KeyExpireAsync(userListKey, TimeSpan.FromDays(30));
                await _db.KeyDeleteAsync(guestListKey);
            }

            // --- 2. Merge "watched_weights" (Kiểu SORTED SET) ---
            var guestWeightKey = $"guest:{guestId}:watched_weights";
            var userWeightKey = $"user:{userId}:watched_weights";

            var guestWeightData = await _db.SortedSetRangeByRankWithScoresAsync(guestWeightKey);
            if (guestWeightData.Length > 0)
            {
                foreach (var item in guestWeightData)
                {
                    // Dùng Increment để cộng dồn điểm cũ của guest vào điểm hiện tại của user
                    await _db.SortedSetIncrementAsync(userWeightKey, item.Element, item.Score);
                }
                await _db.KeyExpireAsync(userWeightKey, TimeSpan.FromDays(30));
                await _db.KeyDeleteAsync(guestWeightKey);
            }
        }

        #endregion

    }


}
