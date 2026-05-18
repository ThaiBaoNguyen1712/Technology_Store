using Tech_Store.Models.Enums;

namespace Tech_Store.Models.Constants;

public static class UserProductEventWeights
{
    public static readonly IReadOnlyDictionary<string, double> Defaults =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [UserProductEventType.ViewDetail] = 1d,
            [UserProductEventType.SearchClick] = 2d,
            [UserProductEventType.WishlistAdd] = 4d,
            [UserProductEventType.AddCart] = 6d,
            [UserProductEventType.Purchase] = 8d,
            [UserProductEventType.RemoveCart] = -3d,
            [UserProductEventType.WishlistRemove] = -2d,
            [UserProductEventType.HomepageClick] = 1d,
            [UserProductEventType.RecommendationClick] = 2d,
        };

    public static double Resolve(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        return Defaults.TryGetValue(eventType, out var weight) ? weight : 0d;
    }
}
