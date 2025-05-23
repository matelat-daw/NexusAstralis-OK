﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Models.Stars;
using NexusAstralis.Models.User;
using System;
using System.Collections.Generic;

namespace NexusAstralis.Data;

public partial class NexusStarsContext(DbContextOptions<NexusStarsContext> options) : DbContext(options)
{
    public virtual DbSet<Constellations> constellations { get; set; }

    public virtual DbSet<Stars> stars { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<NexusUser>();
        modelBuilder.Entity<Constellations>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__constell__3213E83F34CE0D39");

            entity.Property(e => e.id).ValueGeneratedNever();

            entity.HasMany(d => d.star).WithMany(p => p.constellation)
                .UsingEntity<Dictionary<string, object>>(
                    "constellation_stars",
                    r => r.HasOne<Stars>().WithMany()
                        .HasForeignKey("star_id")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_constellation_stars_star"),
                    l => l.HasOne<Constellations>().WithMany()
                        .HasForeignKey("constellation_id")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_constellation_stars_constellation"),
                    j =>
                    {
                        j.HasKey("constellation_id", "star_id");
                        j.ToTable("constellation_stars", t => t.ExcludeFromMigrations());
                    });
        });

        modelBuilder.Entity<Stars>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__stars__3213E83F20EF987F");

            entity.Property(e => e.id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Constellations>().ToTable("constellations", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Stars>().ToTable("stars", t => t.ExcludeFromMigrations());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}