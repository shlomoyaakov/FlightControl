
using System.Text.Json.Serialization;

namespace FlightControlWeb.Models
{
    public class Server
    {
        [JsonPropertyName("ServerId")]
        public string ServerId { get; set; }

        [JsonPropertyName("ServerURL")]
        public string ServerUrl { get; set; }
    }
}
