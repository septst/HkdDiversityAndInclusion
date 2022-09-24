using Microsoft.EntityFrameworkCore;
using WebApi.Models;

namespace WebApi.Infrastructure;

public class Context : DbContext
{
    public Context(DbContextOptions<Context> options) : base(options)
    {
    }

    public DbSet<Candidate> Candidates { get; set; } = null!;
    public DbSet<Rule> Rules { get; set; } = null!;
}