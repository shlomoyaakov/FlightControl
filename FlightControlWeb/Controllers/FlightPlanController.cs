using System;
using System.Threading.Tasks;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlightControlWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightPlanController : ControllerBase
    {
        private IFlightPlanManager flightPlanManager;
        public FlightPlanController(IFlightPlanManager fm)
        {
            flightPlanManager = fm;
        }


        /*GET: api/FlightPlan/{id}
         * Get flightplan by id and if the flightplan is
         * from a remote server then request from the Appropriate server.
         * If the flightplan was not found the client gets a bad request.
         */
        [HttpGet("{id}", Name = "Get")]
        public async Task<IActionResult> Get(string id)
        {
            FlightPlan flightPlan;
            try
            {
                flightPlan = await flightPlanManager.GetFlightPlanAsync(id);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
            return Ok(flightPlan);    
        }

        /*  POST: api/FlightPlane
         * Gets a Flightplan from the client if the flightplan is valid 
         * the flightplan is saved in the database and the cache, and if not
         * the client gets a bad request.
         */
        [HttpPost]
        public IActionResult Post([FromBody] FlightPlan value)
        {
            try
            {
                flightPlanManager.AddFlightPlan(value);
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
            return Ok();
        }
    }
}
