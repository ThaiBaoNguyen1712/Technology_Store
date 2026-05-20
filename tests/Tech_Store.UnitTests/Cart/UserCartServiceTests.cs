using Tech_Store.UnitTests.Builders;
using Tech_Store.UnitTests.Infrastructure;

namespace Tech_Store.UnitTests.Cart
{
    public class UserCartServiceTests
    {
        [Fact]
        public async Task AddToCartAsync_creates_cart_and_adds_item_when_stock_is_available()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().Build();
            var product = new ProductBuilder().WithId(10).WithStock(5).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithStock(5).WithPrice(150_000m).Build();

            context.Users.Add(user);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            await context.SaveChangesAsync();

            var tracker = new Mock<IUserProductEventTrackingService>();
            var service = new UserCartService(context, tracker.Object);

            var result = await service.AddToCartAsync(user.UserId, variant.VarientId, 2);

            result.Success.Should().BeTrue();
            var cart = await context.Carts.Include(x => x.CartItems).SingleAsync();
            cart.CartItems.Should().ContainSingle();
            cart.CartItems.Single().VarientProductId.Should().Be(variant.VarientId);
            cart.CartItems.Single().Quantity.Should().Be(2);
            tracker.Verify(
                x => x.TrackAsync(It.Is<UserProductEventWriteRequest>(r => r.EventType == UserProductEventType.AddCart), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddToCartAsync_returns_failure_when_variant_stock_is_not_enough()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().Build();
            var product = new ProductBuilder().WithId(10).WithStock(10).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithStock(1).Build();

            context.Users.Add(user);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            await context.SaveChangesAsync();

            var service = new UserCartService(context, Mock.Of<IUserProductEventTrackingService>());

            var result = await service.AddToCartAsync(user.UserId, variant.VarientId, 2);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Tồn kho");
            context.CartItems.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateQuantityAsync_updates_item_when_stock_is_available()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().Build();
            var product = new ProductBuilder().WithId(10).WithStock(10).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithStock(10).Build();
            var cart = new Models.Cart { UserId = user.UserId };
            var item = new CartItem { Cart = cart, Product = product, ProductId = product.ProductId, VarientProduct = variant, VarientProductId = variant.VarientId, Quantity = 1 };

            context.Users.Add(user);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            context.Carts.Add(cart);
            context.CartItems.Add(item);
            await context.SaveChangesAsync();

            var service = new UserCartService(context, Mock.Of<IUserProductEventTrackingService>());

            var result = await service.UpdateQuantityAsync(user.UserId, variant.VarientId, 4);

            result.Success.Should().BeTrue();
            (await context.CartItems.SingleAsync()).Quantity.Should().Be(4);
        }

        [Fact]
        public async Task UpdateQuantityAsync_returns_failure_when_product_stock_is_not_enough()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().Build();
            var product = new ProductBuilder().WithId(10).WithStock(2).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).WithStock(10).Build();
            var cart = new Models.Cart { UserId = user.UserId };
            var item = new CartItem { Cart = cart, Product = product, ProductId = product.ProductId, VarientProduct = variant, VarientProductId = variant.VarientId, Quantity = 1 };

            context.Users.Add(user);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            context.Carts.Add(cart);
            context.CartItems.Add(item);
            await context.SaveChangesAsync();

            var service = new UserCartService(context, Mock.Of<IUserProductEventTrackingService>());

            var result = await service.UpdateQuantityAsync(user.UserId, variant.VarientId, 3);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Tồn kho");
            (await context.CartItems.SingleAsync()).Quantity.Should().Be(1);
        }

        [Fact]
        public async Task RemoveItemAsync_removes_item_and_tracks_remove_event()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            var user = new UserBuilder().Build();
            var product = new ProductBuilder().WithId(10).Build();
            var variant = new VariantBuilder().WithId(101).ForProduct(product).Build();
            var cart = new Models.Cart { UserId = user.UserId };
            var item = new CartItem { Cart = cart, Product = product, ProductId = product.ProductId, VarientProduct = variant, VarientProductId = variant.VarientId, Quantity = 1 };

            context.Users.Add(user);
            context.Products.Add(product);
            context.VarientProducts.Add(variant);
            context.Carts.Add(cart);
            context.CartItems.Add(item);
            await context.SaveChangesAsync();

            var tracker = new Mock<IUserProductEventTrackingService>();
            var service = new UserCartService(context, tracker.Object);

            var result = await service.RemoveItemAsync(user.UserId, variant.VarientId);

            result.Success.Should().BeTrue();
            context.CartItems.Should().BeEmpty();
            tracker.Verify(
                x => x.TrackAsync(It.Is<UserProductEventWriteRequest>(r => r.EventType == UserProductEventType.RemoveCart), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
