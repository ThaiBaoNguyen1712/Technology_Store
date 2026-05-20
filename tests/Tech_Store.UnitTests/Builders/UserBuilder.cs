namespace Tech_Store.UnitTests.Builders
{
    internal sealed class UserBuilder
    {
        private readonly User _user = new()
        {
            UserId = 1,
            Email = "buyer@techstore.test",
            FirstName = "Test",
            LastName = "Buyer",
            PhoneNumber = "0123456789",
            Img = "none.png",
            IsActive = true,
            IsVerified = true,
            PasswordHash = PasswordHelper.HashPassword("Password123!")
        };

        public UserBuilder WithId(int id)
        {
            _user.UserId = id;
            return this;
        }

        public UserBuilder WithEmail(string email)
        {
            _user.Email = email;
            return this;
        }

        public UserBuilder WithPassword(string password)
        {
            _user.PasswordHash = PasswordHelper.HashPassword(password);
            return this;
        }

        public UserBuilder Inactive()
        {
            _user.IsActive = false;
            return this;
        }

        public User Build() => _user;
    }
}
