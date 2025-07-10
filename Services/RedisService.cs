using StackExchange.Redis;
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

    }


}
