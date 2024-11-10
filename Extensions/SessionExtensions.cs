using System.Text.Json;

namespace Tech_Store.Extensions
{
    public static class SessionExtensions
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            var jsonData = JsonSerializer.Serialize(value);
            session.SetString(key, jsonData);
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var jsonData = session.GetString(key);
            if (string.IsNullOrEmpty(jsonData)) return default;
            return JsonSerializer.Deserialize<T>(jsonData);
        }
    }
}
