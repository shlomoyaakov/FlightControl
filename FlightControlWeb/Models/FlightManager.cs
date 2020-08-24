using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace FlightControlWeb.Models
{
    public class FlightManager : IFlightManager
    {
        private readonly object balanceLock;
        private SQLiteDBContext db = new SQLiteDBContext();
        private IScheduledCache scheduledCache;
        public FlightManager (IScheduledCache sc)
        {
            scheduledCache = sc;
            balanceLock = sc.GetLock();
        }

         /*
         * Delete the flightplan with this id from the cache and the data base.
         * if there isn't flightplan with this id then an exception is being
         * thrown.
         */
        public void DeleteFlight(string id)
        {
            DeleteFromCache(id);
            try
            {
                DeleteFromDb(id);
            }
            catch(Exception e)
            {
                throw e;
            }

        }
        
        /*  Delete flightPlan from the cache using id.
         * If the flightplan was not found then an exception is being thrown.
         */
        private void DeleteFromDb(string id)
        {
            try
            {
                lock (balanceLock)
                {
                    db.FlightPlanes.Remove(db.FlightPlanes.Find(id));
                    db.SaveChanges();
                }
            }
            catch
            {
                throw new Exception("ID: " + id + " doesn't exist");
            }

        }

        // Delete flight plan, if it exists, from the cache.
        private void DeleteFromCache(string id)
        {
            List<Tuple<string, FlightPlan>> cacheList = null;
            lock (balanceLock)
                cacheList = (List<Tuple<string, FlightPlan>>)scheduledCache.TryGetValue("FlightPlan");
            if (cacheList == null)
                return;
            List<Tuple<string, FlightPlan>> list = new List<Tuple<string, FlightPlan>>();
            lock (balanceLock)
                list.AddRange(cacheList);
            Tuple<string, FlightPlan> _tuple = null;
            foreach (Tuple<string, FlightPlan> tuple in list)
            {
                if (tuple.Item1.Equals(id))
                {
                    _tuple = tuple;
                    break;
                }
            }
            if (_tuple != null)
            {
                lock (balanceLock)
                    cacheList.Remove(_tuple);
            }
        }

        /* Gets flights asynchronous, relative to the variable date, from the server and from list of remote servers 
         * that the server holds.
         * if the date is invalid then an exception is thrown.
         */
        public async Task<Flight[]> GetAllFlightsRelativeToAsync(string date)
        {
            List<Task<Flight[]>> tasks = new List<Task<Flight[]>>();
            try
            {
                //Gets flights from the server.
                tasks.Add(GetFlightsRelativeToAsync(date));
                //Gets the flights from remote server
                tasks.Add(GetFlightsFromServerAsync(date));
            }catch(Exception e)
            {
                throw e;
            }
            var result = await Task.WhenAll(tasks);
            List<Flight> retList = new List<Flight>();
            //units the results.
            foreach(var item in result)
            {
                retList.AddRange(item);
            }
            Flight[] flights = retList.ToArray();
            return flights;
        }

        /*
         * Asks for flights relative to date from each remote server in list async,
         * and then combine the result and return an array of flight.
         */
        private async Task<Flight[]> GetFlightsFromServerAsync(string date)
        {
            Flight[] flight;
            List<Server> list;
            lock (balanceLock)
              list= db.Servers.ToList();
            if (list == null)
                return new Flight[0];
            List<Task<List<Flight>>> tasks = new List<Task<List<Flight>>>();
            foreach (Server server in list)
            {
                //request for flights realive to from each server
                tasks.Add(GetFlightFromServerAsync(server, date));
            }
            //wait for all the request to be finished.
            var result = await Task.WhenAll(tasks);
            List<Flight> retList = new List<Flight>();
            //combine the results.
            foreach (var item in result)
            {
                retList.AddRange(item);
            }
            flight = retList.ToArray();
            for(int i = 0; i < flight.Length; i++)
            {
                //mark that the flights are external.
                flight[i].IsExternal = true;
            }
            return flight;
        }

        /*
         * specify for each external flight the server that it came from,
         * in order to get the appropiate flightplan.
         */
        private void LinkBetweenFlightIdAndServerId(List<Flight> list,Server server)
        {
            string url = server.ServerUrl;
            string serverId = server.ServerId;
            foreach(Flight flight in list)
            {
                lock (balanceLock)
                {
                    scheduledCache.Set(flight.Id, url);
                    scheduledCache.Set(serverId, flight.Id);
                }
            }
        }
        
        /*
         * Gets from remote server flights relative to date, using http client
         */
        private async Task<List<Flight>> GetFlightFromServerAsync(Server server,string date)
        {
            string url = server.ServerUrl;
            Flight[] deserialized = new Flight[0];
            List<Flight> retList = new List<Flight>();
            HttpClient client = new HttpClient();
            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(url + "/api/Flights?relative_to=" + date
                , HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }catch(Exception)
            {
                return retList;
            }
            
            if (response != null && response.IsSuccessStatusCode)
            {
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new CustomContractResolver();
                // Get the response
                var customerJsonString = await response.Content.ReadAsStringAsync();
                // Deserialise the data
                 deserialized = JsonConvert.DeserializeObject<Flight[]>(custome‌​rJsonString,settings);
                retList.AddRange(deserialized);
            }
            LinkBetweenFlightIdAndServerId(retList, server);
            return retList;
        }
     
        /*
         * Get flights from the server  realtive to date.
         * If date is invalid then exception is being thrown.
         */
        public async  Task<Flight[]> GetFlightsRelativeToAsync(string date)
        { 
            Regex r1 = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z");
            Match m1 = r1.Match(date);
            if (!m1.Success)
                throw new Exception("Date: "+date+" has wrong format");
            DateTime dt1;
            try
            {
                dt1 = DateTime.Parse(date);
            }catch(Exception)
            {
                throw new Exception("Date: " + date + " has wrong format");
            }
            return await Task.Run(()=>GetsFlightsHelper(dt1)); 
        }

        /*
         * Gets all the flightplan from the database and convert them to
         * List<Tuple<string,FlightPlan>> the string represent the flightplan's id.
         */
        private List<Tuple<string,FlightPlan>> GetFlightPlanFromDb()
        {
            List<JsonFlightPlan> list;
            lock (balanceLock)
                list = db.FlightPlanes.ToList();
            List<Tuple<string,FlightPlan>> retList = new List<Tuple<string,FlightPlan>>();
            if (list == null)
                return retList;
            //convert to List<Tuple<string,FlightPlan>>
            foreach(JsonFlightPlan jsFlighPlan in list)
            {
                if (jsFlighPlan == null)
                    continue;
                string jsonString = jsFlighPlan.Json;
                string id = jsFlighPlan.Id;
                if (jsonString == null||id == null)
                    continue;
                retList.Add(new Tuple<string,FlightPlan>(id,
                    JsonConvert.DeserializeObject<FlightPlan>(jsonString)));
            }
            return retList;
        }

        /*
         * Get all the flights realtive to DatTime dt1.
         * create flight object for each appropiate Flight Plan
         */
        private Flight[] GetsFlightsHelper(DateTime dt1)
        {
            List<Tuple<string, FlightPlan>> list = GetFlightPlanFromDb();
            Flight flight = null;
            List<Flight> flightList = new List<Flight>();
            foreach (Tuple<string, FlightPlan> tuple in list)
            {
                FlightPlan flightPlan = tuple.Item2;
                if (flightPlan == null)
                    continue;
                DateTime dt2 = DateTime.Parse(flightPlan.InitialLocation.DateTime);
                if (DateTime.Compare(dt1, dt2) < 0)
                    continue;
                flight = CheckFlightPlan(flightPlan, dt1, dt2, tuple.Item1);
                if (flight != null)
                {
                    flightList.Add(flight);
                }
            }
            return flightList.ToArray();
        }

        /*
         * check if the Flightplan with dt2 inital date time is relevant to Datetime dt1,
         * if it is then the function return Flight object((using linear interpolation) from this flightplan ,and if not
         * the flight plan return null.
         */
        private Flight CheckFlightPlan(FlightPlan flightPlan, DateTime dt1, DateTime dt2,string id)
        {
            double lastLong = flightPlan.InitialLocation.Longitude;
            double lastLat = flightPlan.InitialLocation.Latitude;
            Segment[] segments = flightPlan.Segments;
            if (segments == null)
                return null;
            foreach(Segment segment in segments)
            {
                if (DateTime.Compare(dt2.AddSeconds(segment.TimespanSeconds), dt1) >= 0)
                {
                    //linear interpolation
                    double x = dt1.Subtract(dt2).TotalSeconds;
                    double deltaX = dt2.AddSeconds(segment.TimespanSeconds).Subtract(dt2).TotalSeconds;
                    double frac = x / deltaX, x1 = (segment.Latitude - lastLat), y1 = (segment.Longitude - lastLong);
                    double angle = Math.Atan(x1 / y1), z1, z2, longitiude, latitude, distance;
                    if (angle < 0)
                    {
                        angle *= -1;
                    }
                    distance = Math.Sqrt(x1 * x1 + y1 * y1)*frac;
                    z1 = distance * Math.Sin(angle);
                    z2 = distance * Math.Cos(angle);
                    if (x1 < 0)
                    {
                        z1 *= -1;
                    }
                    if (y1 < 0)
                    {
                        z2 *= -1;
                    }
                    longitiude = lastLong + z2;
                    latitude = lastLat + z1;
                    //creates the flightPlan
                    return CreateFlight(latitude, longitiude, flightPlan, id,dt1);
                }
                lastLat = segment.Latitude;
                lastLong = segment.Longitude;
                dt2 = dt2.AddSeconds(segment.TimespanSeconds);
            }
            return null;
        }

        //create flightplan with using this parameters
        Flight CreateFlight(double lat,double longt,FlightPlan flightPlane,string id ,DateTime dt)
        {
            Flight flight = new Flight();
            flight.DateTime = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            flight.IsExternal = false;
            flight.Id = id;
            flight.Latitude = lat;
            flight.Longitude = longt;
            flight.Passengers = flightPlane.Passengers;
            flight.CompanyName = flightPlane.CompanyName;
            return flight;
        }   
    } 
}
