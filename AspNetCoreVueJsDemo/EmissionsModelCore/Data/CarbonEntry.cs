using System;
using System.ComponentModel.DataAnnotations;

namespace Emissions.Data {
    public class CarbonEntry {
        public long Id { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ApplicationUser User { get; set; }

        public string UserId { get; set; }

        [Range(typeof(DateTime), "1900-01-01T00:00:00Z", "2900-01-01T00:00:00Z", ErrorMessage = "Value for {0} must be between {1} and {2}")]
        public DateTime EmittedTimestamp { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime CreationTimestamp { get; set; }

        [Required(AllowEmptyStrings = false)] 
        [StringLength(100)]
        public string Name { get; set; }

        [Range(1, 99999)]
        public double Emissions { get; set; }

        [Range(0, 99999)]
        public decimal? Price { get; set; }
    }
}
