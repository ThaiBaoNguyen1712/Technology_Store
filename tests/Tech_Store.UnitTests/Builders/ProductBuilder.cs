namespace Tech_Store.UnitTests.Builders
{
    internal sealed class ProductBuilder
    {
        private readonly Product _product = new()
        {
            ProductId = 100,
            ProductSysId = "prd-100",
            Name = "iPhone Test",
            SellPrice = 100_000m,
            OriginalPrice = 120_000m,
            Stock = 10,
            Status = "available",
            Visible = true
        };

        public ProductBuilder WithId(int id)
        {
            _product.ProductId = id;
            _product.ProductSysId = $"prd-{id}";
            return this;
        }

        public ProductBuilder WithName(string name)
        {
            _product.Name = name;
            return this;
        }

        public ProductBuilder WithPrice(decimal price)
        {
            _product.SellPrice = price;
            return this;
        }

        public ProductBuilder WithStock(int stock)
        {
            _product.Stock = stock;
            return this;
        }

        public ProductBuilder WithStatus(string status)
        {
            _product.Status = status;
            return this;
        }

        public Product Build() => _product;
    }

    internal sealed class VariantBuilder
    {
        private readonly Tech_Store.Models.VarientProduct _variant = new()
        {
            VarientId = 1000,
            ProductId = 100,
            Sku = "SKU-1000",
            Price = 100_000m,
            Stock = 10,
            Attributes = "128GB"
        };

        public VariantBuilder WithId(int id)
        {
            _variant.VarientId = id;
            _variant.Sku = $"SKU-{id}";
            return this;
        }

        public VariantBuilder ForProduct(Product product)
        {
            _variant.ProductId = product.ProductId;
            _variant.Product = product;
            return this;
        }

        public VariantBuilder WithPrice(decimal price)
        {
            _variant.Price = price;
            return this;
        }

        public VariantBuilder WithStock(int stock)
        {
            _variant.Stock = stock;
            return this;
        }

        public Tech_Store.Models.VarientProduct Build() => _variant;
    }
}
