using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApi.Data.Configurations;

public class WeatherForecastConfig : IEntityTypeConfiguration<MyApi.WeatherForecast>
{
    public void Configure(EntityTypeBuilder<MyApi.WeatherForecast> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Summary).HasMaxLength(128);
        builder.HasIndex(w => w.Date);
    }
}


