using Microsoft.EntityFrameworkCore;

namespace MyApi;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
    public DbSet<WeatherAnalysis> WeatherAnalyses => Set<WeatherAnalysis>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudyMessage> StudyMessages => Set<StudyMessage>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudyGoal> StudyGoals => Set<StudyGoal>();
    public DbSet<Concept> Concepts => Set<Concept>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
