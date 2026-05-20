using Tech_Store.UnitTests.Builders;
using Tech_Store.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tech_Store.UnitTests.Auth
{
    public class AuthenticationFlowServiceTests
    {
        [Fact]
        public async Task AuthenticateUserAsync_returns_user_for_valid_active_credentials()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            context.Users.Add(new UserBuilder().WithEmail("active@techstore.test").WithPassword("Secret123!").Build());
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.AuthenticateUserAsync("active@techstore.test", "Secret123!");

            result.IsSuccess.Should().BeTrue();
            result.User.Should().NotBeNull();
            result.User!.Email.Should().Be("active@techstore.test");
        }

        [Fact]
        public async Task AuthenticateUserAsync_returns_failure_for_invalid_password()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            context.Users.Add(new UserBuilder().WithEmail("active@techstore.test").WithPassword("Secret123!").Build());
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.AuthenticateUserAsync("active@techstore.test", "WrongPassword!");

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task AuthenticateUserAsync_returns_failure_for_inactive_user()
        {
            using var db = new SqliteTestDb();
            await using var context = db.CreateContext();
            context.Users.Add(new UserBuilder().WithEmail("blocked@techstore.test").WithPassword("Secret123!").Inactive().Build());
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.AuthenticateUserAsync("blocked@techstore.test", "Secret123!");

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().NotBeNullOrWhiteSpace();
        }

        private static AuthenticationFlowService CreateService(ApplicationDbContext context)
        {
            var emailService = new Mock<IEmailService>();
            var mediator = new Mock<IMediator>();
            var redisDb = new Mock<IDatabase>();
            var redis = new Mock<IConnectionMultiplexer>();
            redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDb.Object);

            return new AuthenticationFlowService(
                context,
                emailService.Object,
                new NotificationService(context, mediator.Object),
                new RedisService(redis.Object, context, NullLogger<RedisService>.Instance));
        }
    }
}
