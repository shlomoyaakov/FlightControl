using FlightControlWeb.Controllers;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Hosting;
using System;

namespace XUnitTestFlightControl
{
    public class ServerControllerTest
    {
        /*
         * Checks if the controller return the list of remote server that the
         * server holds, and verify its size.
         */
        [Fact]
        public void GetTest()
        {
            // Arrange
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(repo => repo.HttpContext.Request.Host)
               .Returns(new HostString("localhost:44351"));
            var serverManagerMock = new Mock<IServerManager>();
            serverManagerMock.Setup(repo => repo.GetServers())
                .Returns(GetServerTest());
            var serverController = new ServersController(serverManagerMock.Object, httpContextAccessor.Object);

            //act
            var result = serverController.Get();

            //assert
            Assert.Equal(2, result.Count);
        }

        /*
         * Test the controller response when the client posts an invalid server
         */
        [Fact]
        public void InvalidPostTest()
        {
            //Arange
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(repo => repo.HttpContext.Request.Host)
               .Returns(new HostString("localhost:44351"));
            var stub = new Mock<IScheduledCache>();
            var serverManager = new ServerManager(stub.Object);
            var server = new Server()
            {
                ServerId = null,
                ServerUrl = "https://localhost:44353"
             };
            var serverController = new ServersController(serverManager,httpContextAccessor.Object);
            //act
            var result = serverController.Post(server);

            //assert
            var action = Assert.IsType<BadRequestObjectResult>(result);
            var message = Assert.IsAssignableFrom<string>(action.Value);
            Assert.Equal("Invalid server", message);
        }

        //return list of two servers.
        private List<Server> GetServerTest()
        {
            List<Server> list = new List<Server>();
            list.Add(new Server()
            {
                ServerId = "AA1111",
                ServerUrl= "https://localhost:44352"
            });
            list.Add(new Server()
            {
                ServerId = "AA1111",
                ServerUrl = "https://localhost:44353"
            });
            return list;
        }
    }
}
