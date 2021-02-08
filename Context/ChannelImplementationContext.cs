using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DiscordTagger
{
    public partial class ChannelImplementationContext : DbContext
    {
        public ChannelImplementationContext()
        {
        }

        public ChannelImplementationContext(DbContextOptions<ChannelImplementationContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ImplementationItem> ImplementationItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var sqlToken = Environment.GetEnvironmentVariable("SqlPassword");

                optionsBuilder.UseNpgsql(String.Format("Host=ec2-34-254-69-72.eu-west-1.compute.amazonaws.com;" +
                    "Database=d336lvftlkdog3;" +
                    "Username=zkcetlqlttffqo;" +
                    "Password={0};" +
                    "SSL Mode = Require;" +
                    "Trust Server Certificate=true ", sqlToken));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "en_US.UTF-8");

            modelBuilder.Entity<ImplementationItem>(entity =>
            {
                entity.HasKey(e => e.ChannelId)
                    .HasName("implementation_items_pk");

                entity.ToTable("implementation_items", "channel_implementation");

                entity.HasIndex(e => new { e.ServerId, e.GroupId }, "implementation_item_server1_id_idx");

                entity.HasIndex(e => new { e.ServerId, e.ChannelId }, "implementation_item_server2_id_idx");

                entity.Property(e => e.ServerId)
                    .ValueGeneratedNever()
                    .HasColumnName("server_id");

                entity.Property(e => e.ChannelId).HasColumnName("channel_id");

                entity.Property(e => e.GroupId).HasColumnName("group_id");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
