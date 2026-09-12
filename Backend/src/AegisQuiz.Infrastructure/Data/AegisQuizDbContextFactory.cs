using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AegisQuiz.Infrastructure.Data
{
    public class AegisQuizDbContextFactory : IDesignTimeDbContextFactory<AegisQuizDbContext>
    {
        public AegisQuizDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AegisQuizDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Database=aegisquiz;Username=postgres;Password=postgres_aegis_secure_pass_2026");
            return new AegisQuizDbContext(optionsBuilder.Options);
        }
    }
}
