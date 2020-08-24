using System.Threading.Tasks;

namespace FlightControlWeb.Models
{
    public interface IFlightPlanManager
    {
        void AddFlightPlan(FlightPlan fPlane);

        Task<FlightPlan> GetFlightPlanAsync(string id);
    }
}
