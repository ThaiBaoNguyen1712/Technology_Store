namespace Tech_Store.UnitTests.Infrastructure
{
    internal sealed class SqliteTestDb : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteTestDb()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _connection.CreateFunction("getdate", () => DateTime.Now);
        }

        public ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
