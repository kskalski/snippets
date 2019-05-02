using Google.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ProtoGardenEF {
  class Database : DbContext {
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
      optionsBuilder.UseSqlite("Data Source=garden.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<Models.Fountain>()
        .Property(m => m.SerialNr)
        .HasConversion(v => v.ToByteArray(), v => ByteString.CopyFrom(v));
      modelBuilder.Entity<Models.Fountain>()
        .Property(m => m.LastRun)
        .HasConversion(v => v.ToDateTimeOffset(), v => Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(v));
      modelBuilder.Entity<Models.Fountain>()
        .Property(m => m.RunFor)
        .HasConversion(v => v.ToTimeSpan(), v => Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(v));

      /* 
       * I would love to use map<int64, Garden>, which generates a MapField<long, Garden> as
       * navigational property, where Garden <- Flower relationship is represenced using Flowers' ids.
       * But it's not possible at the moment.
       * 
         modelBuilder.Entity<Models.Garden>()
        .Property(m => m.Flowers)
        .HasConversion(v => v.Values, v => v.ToDictionary(flower => flower.Id, flower => flower));
        */
    }

    public DbSet<Models.Fruit> Fruits { get; set; }
    public DbSet<Models.Tree> Trees { get; set; }

    public DbSet<Models.Garden> Gardens { get; set; }

    public DbSet<Models.Fountain> Fountains { get; set; }

    public DbSet<Models.Flower> Flowers { get; set; }
  }
}
