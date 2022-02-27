using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Emissions.Utils {
    public class ExplicitUtcFormatDateTimeConverter : JsonConverter<DateTime> {
        public override DateTime Read(ref Utf8JsonReader reader, Type type_to_convert, JsonSerializerOptions options) {
            Debug.Assert(type_to_convert == typeof(DateTime));
            using var json_doc = JsonDocument.ParseValue(ref reader);
            var txt = json_doc.RootElement.GetRawText().Trim('"').Trim('\'');
            return Dates.ConvertStringToDateTimeUTC(txt);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    public class Dates {
        public static DateTime ConvertStringToDateTimeUTC(string value) {
            var datetime = DateTime.Parse(value);
            if (datetime.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
            return datetime.ToUniversalTime();
        }

        public static DateTimeOffset MoveUpToNearestMidnight(DateTimeOffset timestamp) {
            if (timestamp.TimeOfDay == TimeSpan.Zero)
                return timestamp;
            return timestamp.AddDays(1).Subtract(timestamp.TimeOfDay);
        }
        public static DateTimeOffset MoveUpToNearestMonthStart(DateTimeOffset timestamp) {
            if (timestamp.Day == 1 && timestamp.TimeOfDay == TimeSpan.Zero)
                return timestamp;
            return timestamp.AddMonths(1).AddDays(1 - timestamp.Day).Subtract(timestamp.TimeOfDay);
        }
    }
}
