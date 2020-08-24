using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    public class InitialLocation
    {
        public InitialLocation()
        {
            //helps to determine if the Longitude and Latitude properties were assinged.
            Longitude = -200;
            Latitude = -200;
        }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("date_time")]
        public string DateTime { get; set; }

    }
}
