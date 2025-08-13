using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApi;

public class WeatherAnalysisConfig : IEntityTypeConfiguration<MyApi.WeatherAnalysis>
{
    public void Configure(EntityTypeBuilder<MyApi.WeatherAnalysis> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Analysis).HasMaxLength(4000);
        builder.HasIndex(a => a.CreatedAt);
    }
}


