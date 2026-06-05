using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Extensions
{
    public static class ProductCatalogQueryExtensions
    {
        public static IQueryable<Product> WhereStorefrontAvailable(this IQueryable<Product> query)
        {
            return query.Where(x => x.Status != "outstock" && x.SellPrice > 0);
        }

        public static IQueryable<Product> ApplyStorefrontKeywordSearch(this IQueryable<Product> query, string? keyword)
        {
            var keywords = keyword?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();

            foreach (var word in keywords)
            {
                var currentWord = word;
                query = query.Where(p =>
                    p.Name.Contains(currentWord) ||
                    (p.Description != null && p.Description.Contains(currentWord)));
            }

            return query;
        }

        public static IQueryable<Product> ApplyStorefrontPriceFilter(this IQueryable<Product> query, string? price)
        {
            return price switch
            {
                "max5" => query.Where(x => x.SellPrice < 5_000_000),
                "max10" => query.Where(x => x.SellPrice >= 5_000_000 && x.SellPrice < 10_000_000),
                "max20" => query.Where(x => x.SellPrice >= 10_000_000 && x.SellPrice < 20_000_000),
                "max50" => query.Where(x => x.SellPrice >= 20_000_000 && x.SellPrice < 50_000_000),
                "more" => query.Where(x => x.SellPrice >= 50_000_000),
                _ => query
            };
        }

        public static IQueryable<Product> ApplyStorefrontSort(this IQueryable<Product> query, string? order)
        {
            return order switch
            {
                "alphabet" => query.OrderBy(x => x.Name),
                "alphabet_desc" => query.OrderByDescending(x => x.Name),
                "price" => query.OrderBy(x => x.SellPrice),
                "price_desc" => query.OrderByDescending(x => x.SellPrice),
                "care" => query.OrderByDescending(x => x.Reviews),
                _ => query.OrderBy(x => x.SortOrder).ThenByDescending(x => x.ProductId)
            };
        }

        public static IQueryable<Brand> ApplyRandomizedBrandOrdering(this IQueryable<Brand> query)
        {
            return query.OrderBy(b => b.SortOrder ?? 0).ThenBy(_ => EF.Functions.Random());
        }
    }
}
