using FlightControlWeb.Controllers;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestFlightControl
{
    public class FlightControllerTests
    {
        /*
         * Checks that the conroller return Flight array when the client asks for Flights array relative to
         * date from the server and from remote servers.
         */
        [Fact]
        public void FlightControllerRelativetoAndSyncAllTest()
        {
            // Arrange
            var context = new Mock<HttpContext>();
            //sync_all specified for the controller to get flights from the server and from remote servers.
            context.SetupGet(x => x.Request.QueryString).Returns(new QueryString("?sync_all"));

            var controllerContext = new ControllerContext()
            {
                HttpContext = context.Object,
            };
            var relative_to = "2020-05-09T18:23:18Z";
            //the controller suppose to use "Get_AllFlights_Relative_ToAsync" method in the IFlightManager object.
            var flightManagerMock = new Mock<IFlightManager>();
            flightManagerMock.Setup(repo => repo.GetAllFlightsRelativeToAsync(relative_to))
                .Returns(TestGetAllFlightsRelativeToAsync());

            var flightsController = new FlightsController(flightManagerMock.Object)
            {
                ControllerContext = controllerContext,
            };

            // Act
            var result = flightsController.GetFlightsAsync(relative_to,"sync_all");
       
            // Assert
            var action = Assert.IsType<OkObjectResult>(result.Result);
            var flightArray = Assert.IsAssignableFrom<Flight[]>(action.Value);
            Assert.Equal(2, flightArray.Length);
        }
        /*
         * Checks that the conroller return Flight array when the client asks for Flights array relative to
         * date from the server.
         */
        [Fact]
        public void FlightControllerRelativetoTest()
        {
            var context = new Mock<HttpContext>();
            //the sync_all isnt part of the request so the controller
            context.SetupGet(x => x.Request.QueryString).Returns(new QueryString("?"));

            var controllerContext = new ControllerContext()
            {
                HttpContext = context.Object,
            };
            // Arrange
            var relative_to = "2020-05-09T18:23:18Z";
            var mockRepo = new Mock<IFlightManager>();
            //the controller suppose to use "GetFlightsRelativeToAsync" method in the IFlightManager object.
            mockRepo.Setup(repo => repo.GetFlightsRelativeToAsync(relative_to))
                .Returns(TestGetAllFlightsRelativeToAsync());

            var flightsController = new FlightsController(mockRepo.Object)
            {
                ControllerContext = controllerContext,
            };

            // Act
            var result = flightsController.GetFlightsAsync(relative_to, "");

            // Assert
            var action = Assert.IsType<OkObjectResult>(result.Result);
            var flightArray = Assert.IsAssignableFrom<Flight[]>(action.Value);
            Assert.Equal(2, flightArray.Length);
        }

        /*
         * Checks if the controller return bad request if the gets invalid Date time in realtive to
         * variable in "Get_FlightsAsync".
         */
        [Fact]
        public void FlightControllerDateTimeTest()
        {
            // Arrange
            var context = new Mock<HttpContext>();
            context.SetupGet(x => x.Request.QueryString).Returns(new QueryString("?"));

            var controllerContext = new ControllerContext()
            {
                HttpContext = context.Object,
            };
            var relative_to = "Invalid_Date_Time";
            var stub = new Mock<IScheduledCache>();
            var flightManager = new FlightManager(stub.Object);
            var flightsController = new FlightsController(flightManager)
            {
                ControllerContext = controllerContext,
            };

            // Act
            var result = flightsController.GetFlightsAsync(relative_to, "");

            // Assert
            var action = Assert.IsType<BadRequestObjectResult>(result.Result);
            var message = Assert.IsAssignableFrom<string>(action.Value);
            Assert.Equal("Date: Invalid_Date_Time has wrong format", message);
        }

        /*
         * Checks if the controller return Not Found if the client asks to delete
         * server that does not exist.
         */
        [Fact]
        public void FlightControllerInvaldIdDeleteTest()
        {
            // Arrange
            var id = "Invalid_ID";
            var flightManagerMock = new Mock<IFlightManager>();
            flightManagerMock.Setup(x => x.DeleteFlight(id)).
                Throws(new Exception("ID: " + id + " was not found"));
            var flightsController = new FlightsController(flightManagerMock.Object);

            // Act
            var result = flightsController.Delete(id);

            // Assert
            var action = Assert.IsType<NotFoundObjectResult>(result);
            var message = Assert.IsAssignableFrom<string>(action.Value);
            Assert.Equal("ID: Invalid_ID was not found", message);
        }
        
        //return Array of two flight.
        private async Task<Flight[]> TestGetAllFlightsRelativeToAsync()
        {
            List<Flight> list = new List<Flight>();
            list.Add(new Flight()
            {
                Id = "AA1111",
                CompanyName = "SwissAir",
                Longitude = 0,
                Latitude = 0,
                DateTime = "2020-05-09T18:23:18Z",
                Passengers = 0,
                IsExternal = false
            });
            list.Add(new Flight()
            {
                Id = "AA2222",
                CompanyName = "SwissAir",
                Longitude = 0,
                Latitude = 0,
                DateTime = "2020-05-09T18:23:18Z",
                Passengers = 0,
                IsExternal = true
            });
            return await Task.Run(() => list.ToArray());
        }
    }
}
