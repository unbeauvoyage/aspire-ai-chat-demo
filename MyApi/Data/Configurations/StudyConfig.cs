using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApi.Data.Configurations;

public class StudySessionConfig : IEntityTypeConfiguration<MyApi.StudySession>
{
    public void Configure(EntityTypeBuilder<MyApi.StudySession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Topic).HasMaxLength(256);
        builder.Property(x => x.CreatedAt);
    }
}

public class StudyMessageConfig : IEntityTypeConfiguration<MyApi.StudyMessage>
{
    public void Configure(EntityTypeBuilder<MyApi.StudyMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => x.SessionId);
        builder.HasOne(x => x.Session)
               .WithMany()
               .HasForeignKey(x => x.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}


