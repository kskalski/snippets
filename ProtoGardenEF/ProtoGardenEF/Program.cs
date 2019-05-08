using Google.Protobuf;
using GPW = Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace ProtoGardenEF {
  class Program {
    static void PopulateDb() {
      using (var db = new Database()) {
        db.Database.EnsureDeleted();
        db.Database.Migrate();

        db.Fruits.Add(new Models.Fruit { Name = "Apple", Weight = 345.2, Taste = Models.Taste.Sour });
        db.Trees.Add(new Models.Tree {
          Height = 45, Fruits = {
            new Models.Fruit { Name = "Banana", Weight = 25.1 }
          }
        });
        db.Gardens.Add(new Models.Garden() {
          Fountain = new Models.Fountain() {
            SerialNr = ByteString.CopyFromUtf8("AbraKadabra"),
            LastRun = GPW.Timestamp.FromDateTime(DateTime.UtcNow),
            RunFor = new GPW.Duration() { Seconds = 34 }
          },
          Trees = { new Models.Tree() { Age = 2 } }
        });
        db.Flowers.Add(new Models.Flower() { IsBlooming = true, NumPetals = 5 });
        db.Flowers.Add(new Models.Flower() { IsBlooming = false, Color = 5 });
        var count = db.SaveChanges();
        Console.WriteLine("{0} records saved to database", count);
      }
    }

    static void ReadDb() {
      using (var db = new Database()) {
        Console.WriteLine("All fruits in database:");
        foreach (var fruit in db.Fruits) {
          Console.WriteLine(" - {0}", fruit);
        }
        Console.WriteLine("All trees in database:");
        foreach (var tree in db.Trees.Include(t => t.Fruits)) {
          Console.WriteLine(" - {0}", tree);
        }
        Console.WriteLine("All flowers in database:");
        foreach (var flower in db.Flowers) {
          Console.WriteLine(" - {0}", flower);
        }
        Console.WriteLine("garden is {0}", db.Gardens.Include(g => g.Fountain).Include(g => g.Trees).First());
      }
    }

    static void SearchDb(string[] args) {
      using (var db = new Database()) {
        if (args.Contains("timestamp_compare")) {
          var recent_fountain_run = db.Fountains
            .Where(f => f.LastRun > GPW.Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2))))
            .Select(f => Tuple.Create(f.Id, f.LastRun)).FirstOrDefault();
          Console.WriteLine("recently run fountain id: {0}", recent_fountain_run);
        }
      }
    }

    static void Main(string[] args) {
      PopulateDb();
      ReadDb();
      SearchDb(args);
    }
  }
}
