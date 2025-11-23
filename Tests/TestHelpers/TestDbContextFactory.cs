using Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Tests.TestHelpers
{
    public static class TestDbContextFactory
    {
        public static LedgerContext CreateInMemoryContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new TestLedgerContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }

    public class TestLedgerContext : LedgerContext
    {
        private readonly DbContextOptions<LedgerContext> _options;

        public TestLedgerContext(DbContextOptions<LedgerContext> options) : base()
        {
            _options = options;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(databaseName: "TestDb");
            }
        }
    }
}
