using Microsoft.EntityFrameworkCore;

namespace MyApi;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
    public DbSet<WeatherAnalysis> WeatherAnalyses => Set<WeatherAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WeatherForecast>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Summary).HasMaxLength(128);
            entity.HasIndex(w => w.Date);
        });

        modelBuilder.Entity<WeatherAnalysis>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Analysis).HasMaxLength(4000);
            entity.HasIndex(a => a.CreatedAt);
        });
    }
}
