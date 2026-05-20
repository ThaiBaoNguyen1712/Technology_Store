using Tech_Store.UnitTests.Builders;
using Tech_Store.UnitTests.Infrastructure;

namespace Tech_Store.UnitTests.Payment
{
    public class ClientPaymentServiceTests
    {
        [Fact]
        public async Task CreateCodOrderAsync_creates_order_calculates_total_and_decrements_stock()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().WithId(1).Build();
            var address = new Address { UserId = user.UserId, AddressLine = "123 Test", Ward = "1", District = "1", Province = "1" };
            var product = new ProductBuilder().WithId(10).WithPrice(100_000m).WithStock(5).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithPrice(100_000m).WithStock(5).Build();
            var cart = new Models.Cart { UserId = user.UserId };
            var cartItem = new CartItem { Cart = cart, Product = product, ProductId = product.ProductId, VarientProduct = variant, VarientProductId = variant.VarientId, Quantity = 2 };

            context.Users.Add(user);
            context.Addresses.Add(address);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            context.Carts.Add(cart);
            context.CartItems.Add(cartItem);
            await context.SaveChangesAsync();

            var httpContext = CreateHttpContext();
            var tracking = new Mock<IUserProductEventTrackingService>();
            var service = CreateService(context, Mock.Of<IOnlinePaymentGatewayService>(), tracking.Object);
            var payment = new PaymentDtoBuilder().WithExistingAddress().AddProduct(product.ProductId, variant.VarientId, 2).Build();

            var result = await service.CreateCodOrderAsync(payment, user.UserId, httpContext);

            result.Success.Should().BeTrue();
            result.RedirectUrl.Should().Be("/Payment/OrderSuccess");

            var order = await context.Orders.Include(x => x.OrderItems).SingleAsync();
            order.OrderStatus.Should().Be(OrderStatusType.Pending.ToRecordValue());
            order.TotalAmount.Should().Be(230_000m);
            order.OriginAmount.Should().Be(200_000m);
            order.ShippingAmount.Should().Be(30_000m);
            order.OrderItems.Should().ContainSingle();

            var paymentRecord = await context.Payments.SingleAsync();
            paymentRecord.Status.Should().Be(PaymentRecordStatusType.Unpaid.ToRecordValue());
            paymentRecord.Amount.Should().Be(230_000m);

            (await context.Products.SingleAsync()).Stock.Should().Be(3);
            (await context.VarientProducts.SingleAsync()).Stock.Should().Be(3);
            context.CartItems.Should().BeEmpty();
            tracking.Verify(x => x.TrackAsync(It.IsAny<UserProductEventWriteRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateCodOrderAsync_returns_conflict_when_stock_is_insufficient()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().WithId(1).Build();
            var address = new Address { UserId = user.UserId, AddressLine = "123 Test", Ward = "1", District = "1", Province = "1" };
            var product = new ProductBuilder().WithId(10).WithPrice(100_000m).WithStock(1).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithPrice(100_000m).WithStock(1).Build();

            context.Users.Add(user);
            context.Addresses.Add(address);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            await context.SaveChangesAsync();

            var service = CreateService(context, Mock.Of<IOnlinePaymentGatewayService>(), Mock.Of<IUserProductEventTrackingService>());
            var payment = new PaymentDtoBuilder().WithExistingAddress().AddProduct(product.ProductId, variant.VarientId, 2).Build();

            var result = await service.CreateCodOrderAsync(payment, user.UserId, CreateHttpContext());

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            context.Orders.Should().BeEmpty();
        }

        [Fact]
        public async Task HandleMomoCallbackAsync_creates_paid_order_when_gateway_validation_succeeds()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().WithId(1).Build();
            var address = new Address { UserId = user.UserId, AddressLine = "123 Test", Ward = "1", District = "1", Province = "1" };
            var product = new ProductBuilder().WithId(10).WithPrice(100_000m).WithStock(5).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithPrice(100_000m).WithStock(5).Build();

            context.Users.Add(user);
            context.Addresses.Add(address);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            await context.SaveChangesAsync();

            var gateway = new Mock<IOnlinePaymentGatewayService>();
            gateway.Setup(x => x.ValidateCallback(PaymentMethodType.Momo, It.IsAny<HttpContext>()))
                .Returns(new OnlinePaymentGatewayCallbackResult { Success = true });

            var httpContext = CreateHttpContext();
            httpContext.Session.SetObject("PaymentInfo", new PaymentDtoBuilder().WithExistingAddress().AddProduct(product.ProductId, variant.VarientId, 2).Build());

            var service = CreateService(context, gateway.Object, Mock.Of<IUserProductEventTrackingService>());

            var result = await service.HandleMomoCallbackAsync(user.UserId, httpContext);

            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCodes.Status200OK);
            var order = await context.Orders.SingleAsync();
            order.OrderStatus.Should().Be(OrderStatusType.Confirmed.ToRecordValue());
            (await context.Payments.SingleAsync()).Status.Should().Be(PaymentRecordStatusType.Paid.ToRecordValue());
            httpContext.Session.GetString("PaymentInfo").Should().BeNull();
            (await context.VarientProducts.SingleAsync()).Stock.Should().Be(3);
        }

        [Fact]
        public async Task HandleMomoCallbackAsync_returns_gateway_failure_for_invalid_signature()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().WithId(1).Build();
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var gateway = new Mock<IOnlinePaymentGatewayService>();
            gateway.Setup(x => x.ValidateCallback(PaymentMethodType.Momo, It.IsAny<HttpContext>()))
                .Returns(new OnlinePaymentGatewayCallbackResult { Success = false, FailureMessage = "invalid signature" });

            var service = CreateService(context, gateway.Object, Mock.Of<IUserProductEventTrackingService>());

            var result = await service.HandleMomoCallbackAsync(user.UserId, CreateHttpContext());

            result.Success.Should().BeFalse();
            result.IsGatewayFailure.Should().BeTrue();
            result.GatewayFailureMessage.Should().Be("invalid signature");
            context.Orders.Should().BeEmpty();
        }

        private static ClientPaymentService CreateService(
            ApplicationDbContext context,
            IOnlinePaymentGatewayService gateway,
            IUserProductEventTrackingService trackingService)
        {
            var mediator = new Mock<IMediator>();
            var settings = new Mock<IPaymentGatewaySettingsService>();
            settings.Setup(x => x.GetGatewaySettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
            settings.Setup(x => x.IsGatewayEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            return new ClientPaymentService(
                context,
                gateway,
                new NotificationService(context, mediator.Object),
                settings.Object,
                trackingService);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext
            {
                Session = new TestSession()
            };
        }
    }
}
