using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace FlightControlWeb.Models
{
    public class CustomContractResolver : DefaultContractResolver
    {
        private Dictionary<string, string> PropertyMappings { get; set; }

        //run time mapping between json and flightplan,flight and server objects.
        public CustomContractResolver()
        {
            this.PropertyMappings = new Dictionary<string, string>
        {
            {"DateTime", "date_time"},
            {"CompanyName", "company_name"},
            {"Latitude", "latitude"},
            {"Id", "flight_id"},
            {"Longitude", "longitude"},
            {"Passengers", "passengers"},
            {"IsExternal", "is_external"},
            {"ServerId" ,"ServerId"},
            {"ServerUrl" ,"ServerURL"},
            {"Segments","segments" },
            {"InitialLocation","initial_location" },
            {"TimespanSeconds","timespan_seconds" }
        };
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            string resolvedName = null;
            var resolved = this.PropertyMappings.TryGetValue(propertyName, out resolvedName);
            return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}
