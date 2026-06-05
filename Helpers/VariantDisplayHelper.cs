using Tech_Store.Models;

namespace Tech_Store.Helpers;

public static class VariantDisplayHelper
{
    public static string Format(VarientProduct? variant)
    {
        if (variant == null)
        {
            return string.Empty;
        }

        return Format(variant.VariantAttributes, variant.Attributes);
    }

    public static string Format(IEnumerable<VariantAttribute>? variantAttributes, string? fallback = null)
    {
        var items = (variantAttributes ?? Enumerable.Empty<VariantAttribute>())
            .Where(x => x.AttributeValue != null)
            .Select(x => new
            {
                AttributeSort = x.AttributeValue.Attribute?.SortOrder ?? int.MaxValue,
                AttributeName = x.AttributeValue.Attribute?.Name?.Trim(),
                Value = x.AttributeValue.Value?.Trim()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .OrderBy(x => x.AttributeSort)
            .ThenBy(x => x.AttributeName)
            .Select(x => string.IsNullOrWhiteSpace(x.AttributeName)
                ? x.Value!
                : $"{x.AttributeName}: {x.Value}")
            .ToList();

        if (items.Count > 0)
        {
            return string.Join(", ", items);
        }

        return fallback?.Trim() ?? string.Empty;
    }
}
