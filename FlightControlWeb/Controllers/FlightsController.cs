using System;
using System.Threading.Tasks;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlightControlWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private IFlightManager flightManager;
        public FlightsController(IFlightManager fm)
        {
            flightManager = fm;
        }

        /* Get array of flight relative to the time and date that the variable relative_to specifies.
         * If sync_all was part of the request then gets flight asynchronous from remote servers.
         * The client gets a bad request if the datetime in relative to was invalid.
         */
        [HttpGet("{sync_all?}")]
        public async Task<IActionResult> GetFlightsAsync([FromQuery]string relative_to, [FromQuery]string sync_all)
        {
            Flight[] flights;
            if (Request.QueryString.Value.Contains("sync_all"))
            {
                //gets flights also from remote servers
                try
                {
                    flights = await flightManager.GetAllFlightsRelativeToAsync(relative_to);
                }
                catch(Exception e)
                {
                    return BadRequest(e.Message);
                }
            }
            else
            {
                //gets flight just from this server.
                try
                {
                    flights = await flightManager.GetFlightsRelativeToAsync(relative_to);
                }
                catch(Exception e)
                {
                    return BadRequest(e.Message);
                }
            }
            return Ok(flights);
        }

        /* DELETE: api/ApiWithActions/{id}
         * Delete the flightplan with this id.
         * if there isn't flightplan with this id then the client gets 
         * Not Found
         */
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                flightManager.DeleteFlight(id);
            }
            catch(Exception e)
            {
                return NotFound(e.Message);
            }
            return Ok();
        }
    }
}
