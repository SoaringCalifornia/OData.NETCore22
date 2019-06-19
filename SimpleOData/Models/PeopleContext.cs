using Microsoft.EntityFrameworkCore;

namespace SimpleOData.Models
{
    public class PeopleContext : DbContext
    {
        public PeopleContext(DbContextOptions<PeopleContext> options) : base(options) { }

        public DbSet<Person> Person { get; set; }
    }
}
