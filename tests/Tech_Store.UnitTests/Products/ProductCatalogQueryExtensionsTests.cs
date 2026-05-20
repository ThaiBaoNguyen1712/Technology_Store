using Tech_Store.UnitTests.Builders;

namespace Tech_Store.UnitTests.Products
{
    public class ProductCatalogQueryExtensionsTests
    {
        [Fact]
        public void WhereStorefrontAvailable_filters_out_outstock_and_non_priced_products()
        {
            var products = new[]
            {
                new ProductBuilder().WithId(1).WithPrice(10_000m).WithStatus("available").Build(),
                new ProductBuilder().WithId(2).WithPrice(10_000m).WithStatus("outstock").Build(),
                new ProductBuilder().WithId(3).WithPrice(0m).WithStatus("available").Build()
            }.AsQueryable();

            var result = products.WhereStorefrontAvailable().ToList();

            result.Select(x => x.ProductId).Should().BeEquivalentTo([1]);
        }

        [Fact]
        public void ApplyStorefrontKeywordSearch_requires_all_keywords_to_match()
        {
            var products = new[]
            {
                new ProductBuilder().WithId(1).WithName("iPhone 15 Pro Max").Build(),
                new ProductBuilder().WithId(2).WithName("iPhone 15").Build(),
                new ProductBuilder().WithId(3).WithName("Galaxy S24 Ultra").Build()
            }.AsQueryable();

            var result = products.ApplyStorefrontKeywordSearch("iPhone Pro").ToList();

            result.Select(x => x.ProductId).Should().BeEquivalentTo([1]);
        }

        [Theory]
        [InlineData("max5", 1)]
        [InlineData("max10", 2)]
        [InlineData("more", 3)]
        public void ApplyStorefrontPriceFilter_returns_expected_band(string filter, int expectedProductId)
        {
            var products = new[]
            {
                new ProductBuilder().WithId(1).WithPrice(4_000_000m).Build(),
                new ProductBuilder().WithId(2).WithPrice(7_000_000m).Build(),
                new ProductBuilder().WithId(3).WithPrice(55_000_000m).Build()
            }.AsQueryable();

            var result = products.ApplyStorefrontPriceFilter(filter).ToList();

            result.Select(x => x.ProductId).Should().BeEquivalentTo([expectedProductId]);
        }

        [Fact]
        public void ApplyStorefrontSort_orders_by_price_descending()
        {
            var products = new[]
            {
                new ProductBuilder().WithId(1).WithPrice(4_000_000m).Build(),
                new ProductBuilder().WithId(2).WithPrice(7_000_000m).Build(),
                new ProductBuilder().WithId(3).WithPrice(5_000_000m).Build()
            }.AsQueryable();

            var result = products.ApplyStorefrontSort("price_desc").ToList();

            result.Select(x => x.ProductId).Should().ContainInOrder(2, 3, 1);
        }
    }
}
