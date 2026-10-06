using Microsoft.EntityFrameworkCore;
using TicketAutomation.Web.Models;

namespace TicketAutomation.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AutomationSettings> Settings => Set<AutomationSettings>();
    public DbSet<TicketJob> Jobs => Set<TicketJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutomationSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<TicketJob>().HasIndex(x => x.ExternalTicketId).IsUnique();
        modelBuilder.Entity<TicketJob>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TicketJob>().Property(x => x.Provider).HasConversion<string>();
    }
}

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Settings.AnyAsync()) return;
        db.Settings.Add(AutomationSettings.CreateDefaults());
        await db.SaveChangesAsync();
    }
}
