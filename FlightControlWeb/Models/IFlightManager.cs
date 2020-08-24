using System.Threading.Tasks;

namespace FlightControlWeb.Models
{
    public interface IFlightManager
    {
        void DeleteFlight(string id);

        Task<Flight[]> GetFlightsRelativeToAsync(string date);

        Task<Flight[]> GetAllFlightsRelativeToAsync(string date);

    }
}
