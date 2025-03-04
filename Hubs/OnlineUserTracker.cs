using System.Collections.Concurrent;

namespace Tech_Store.Hubs
{
    public static class OnlineUserTracker
    {
        private static readonly ConcurrentDictionary<int, string> OnlineUsers = new();

        public static void UserConnected(int userId, string connectionId)
        {
            OnlineUsers[userId] = connectionId;
        }

        public static void UserDisconnected(int userId)
        {
            OnlineUsers.TryRemove(userId, out _);
        }

        public static List<int> GetOnlineUserIds()
        {
            return OnlineUsers.Keys.ToList();
        }
    }
}
