using System.ComponentModel.DataAnnotations;

namespace Emissions.Data {
    public class CarbonEntry {

        public CarbonEntry(): this(new Proto.CarbonEntry()) {
        }
        public CarbonEntry(Proto.CarbonEntry raw) {
            this.Raw = raw;
        }

        public long Id { get => Raw.Id; set => Raw.Id = value; }

        public ApplicationUser User { get; set; }

        public string UserId { get => Raw.UserId; set => Raw.UserId = value; }

        [Range(typeof(DateTime), "1900-01-01T00:00:00Z", "2900-01-01T00:00:00Z", ErrorMessage = "Value for {0} must be between {1} and {2}")]
        public DateTime EmittedTimestamp { get => Raw.EmittedTimestamp.ToDateTime(); set => Raw.EmittedTimestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc)); }

        public DateTime CreationTimestamp { get; set; }

        [Required(AllowEmptyStrings = false)] 
        [StringLength(100)]
        public string Name { get => Raw.Name; set => Raw.Name = value; }

        [Range(1, 99999)]
        public double Emissions { get => Raw.Emissions; set => Raw.Emissions = value; }

        [Range(0, 99999)]
        public double? Price { 
            get => Raw.HasPrice ? Raw.Price : null;
            set {
                if (value != null) Raw.Price = value.Value; else Raw.ClearPrice();
            }
        }

        public readonly Proto.CarbonEntry Raw;
    }
}
