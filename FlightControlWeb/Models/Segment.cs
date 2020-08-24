using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    public class Segment
    {
        public Segment()
        {
            //helps to determine if the TimespanSeconds, Longitude and Latitude properties were assinged.
            Longitude = -200;
            Latitude = -200;
            TimespanSeconds = -1;
        }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("timespan_seconds")]
        public double TimespanSeconds { get; set; }
        
    }
}
