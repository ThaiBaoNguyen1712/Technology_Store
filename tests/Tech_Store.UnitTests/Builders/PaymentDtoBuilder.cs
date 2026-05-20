namespace Tech_Store.UnitTests.Builders
{
    internal sealed class PaymentDtoBuilder
    {
        private readonly PaymentDTo _dto = new()
        {
            VoucherCode = string.Empty,
            Note = "Please handle carefully",
            NewAddress = false,
            Products = new List<Tech_Store.Models.DTO.Payment.Client.VarientProduct>()
        };

        public PaymentDtoBuilder WithExistingAddress()
        {
            _dto.NewAddress = false;
            _dto.Address = null;
            return this;
        }

        public PaymentDtoBuilder WithNewAddress(string line = "123 Test Street")
        {
            _dto.NewAddress = true;
            _dto.Address = new Address
            {
                AddressLine = line,
                Ward = "1",
                District = "1",
                Province = "1"
            };
            return this;
        }

        public PaymentDtoBuilder WithVoucher(string code)
        {
            _dto.VoucherCode = code;
            return this;
        }

        public PaymentDtoBuilder AddProduct(int productId, int variantId, int quantity)
        {
            _dto.Products.Add(new Tech_Store.Models.DTO.Payment.Client.VarientProduct
            {
                ProductId = productId,
                VarientProductID = variantId,
                Quantity = quantity
            });

            return this;
        }

        public PaymentDTo Build() => _dto;
    }
}
