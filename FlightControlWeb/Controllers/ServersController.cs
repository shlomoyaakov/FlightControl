using System;
using System.Collections.Generic;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlightControlWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServersController : ControllerBase
    {
        private IServerManager serverManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private string baseUrl;
        public ServersController(IServerManager sm, IHttpContextAccessor httpContext)
        {
            httpContextAccessor = httpContext;
            serverManager = sm;
            baseUrl = "https://" + httpContextAccessor.HttpContext.Request.Host.Value;
        }


        /* GET: api/servers
         *  Gets the list of remote server that the server is holds.
         */
        [HttpGet]
        public List<Server> Get()
        {
            return serverManager.GetServers();
        }


        // POST: api/servers
        /*
         * Get Server from the client and makes sure that the serverurl isnt the current url.
         * The model checks if is not valid the client gets a bad request,
         * and if is the server is saved.
         */
        [HttpPost]
        public IActionResult Post([FromBody] Server value)
        {
            if (value != null && value.ServerUrl != null)
            {
                if (value.ServerUrl.Equals(this.baseUrl))
                    return BadRequest("The Server URL is identical to the current server URL");
            }
            try
            {
                serverManager.AddServer(value);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            return Ok();
        }


        // DELETE: api/ApiWithActions/{id}
        /*
         * Delete the server with this id, and if
         * there isn't server with such id then the client gets
         * Not Found.
         */
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                serverManager.DeleteServer(id);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
            return Ok();
            ;
        }
    }
}