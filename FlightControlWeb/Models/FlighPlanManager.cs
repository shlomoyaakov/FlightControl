using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace FlightControlWeb.Models
{
    public class FlighPlanManager : IFlightPlanManager
    {
        private IScheduledCache scheduledCache;
        private readonly object balanceLock;
        private SQLiteDBContext db = new SQLiteDBContext();
        public FlighPlanManager(IScheduledCache sc)
        {
            List<Tuple<string, FlightPlan>> list = new List<Tuple<string, FlightPlan>>();
            scheduledCache = sc;
            scheduledCache.Set("FlightPlan", list);
            balanceLock = sc.GetLock();
        }

        //checsks if the date time string in the variable date is valid, using regex and DateTime object.
        private bool CheckDateTime(string date)
        {
            //checks the format
            Regex r1 = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z");
            Match m1 = r1.Match(date);
            if (!m1.Success)
                return false;
            try
            {
                DateTime dt = DateTime.Parse(date);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        //checks if the fligh plan fPlan is valid and all its propeties get value.
        private bool CheckIfFlightPlanValid(FlightPlan fPlan)
        {
            string str = "Flight Plan is invalid";
            if (fPlan.CompanyName == null)
                throw new Exception(str);
            if (fPlan.InitialLocation == null)
                throw new Exception(str);
            if (fPlan.Passengers < 0)
                throw new Exception(str);
            if (fPlan.Segments == null)
                throw new Exception(str);
            if (!(CheckDateTime(fPlan.InitialLocation.DateTime)))
                throw new Exception(str + str + ": in initial_location, date_time is invalid/null");
            if (!(fPlan.InitialLocation.Latitude >= -90 && fPlan.InitialLocation.Latitude <= 90))
                throw new Exception(str + ": Latitude was not assigned or has value out of range -90 to 90 ");
            if (!(fPlan.InitialLocation.Longitude >= -180 && fPlan.InitialLocation.Longitude <= 180))
                throw new Exception(str + ": Longitude was not assigned or has value out of range -180 to 180");
            int i = 0;
            foreach (Segment segment in fPlan.Segments)
            {
                if (!(segment.Latitude >= -90 && segment.Latitude <= 90))
                    throw new Exception(str + ": in Segment[" + i + "] Latitude was not assigned" +
                        " or has value out of range -90 to 90");
                if (!(segment.Longitude >= -180 && segment.Longitude <= 180))
                    throw new Exception(str + ": in Segment[" + i + "] Longitude was not assigned" +
                        " or has value out of range -180 to 180");
                if (segment.TimespanSeconds < 0)
                    throw new Exception(str + ": in Segment[" + i + "] invalid timespan");
                i++;
            }
            return true;
        }

        /* Add flightplan to the server.
         * If the flight Plan has property that wasn't assigned then an appropiate
         * exception is being thrown.
         * after the flightplan was validate the flightplan gets id and saved in the data base and
         * in the cache.
         */
        public void AddFlightPlan(FlightPlan fPlan)
        {
            try
            {
                CheckIfFlightPlanValid(fPlan);
            }
            catch (Exception e)
            {
                throw e;
            }
            SaveFlightPlan(fPlan);
        }

        /*
         * Gets an unUnique id for the flightplan and saves it in the cache and the data base.
         */
        public void SaveFlightPlan(FlightPlan flightPlan)
        {
            List<Tuple<string, FlightPlan>> list = null;
            lock (balanceLock)
                list = (List<Tuple<string, FlightPlan>>)scheduledCache.TryGetValue("FlightPlan");
            string id = getId();
            //the flightplan that being saved in the data base.
            JsonFlightPlan js = new JsonFlightPlan();
            js.Id = id;
            js.Json = Newtonsoft.Json.JsonConvert.SerializeObject(flightPlan);
            lock (balanceLock)
            {
                //save in the data base and cache
                db.FlightPlanes.Add(js);
                db.SaveChanges();
                if(list!=null)
                    list.Add(new Tuple<string, FlightPlan>(id, flightPlan));
            }

        }

        //create unique id with the format "[A-Z][A-Z][0-9][0-9][0=9][0-9]" and return it.
        private string getId()
        {
            string id;
            var capital = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var numbers = "0123456789";
            var str = new char[6];
            Random rnd = new Random();
            for(int i = 0; i < 2; i++)
            {
                str[i] = capital[rnd.Next(capital.Length)];
            }
            for (int i = 2; i < 6; i++)
            {
                str[i] = numbers[rnd.Next(numbers.Length)];
            }
            id = new String(str);
            return id;
        }


        public async Task<FlightPlan> GetFlightPlanAsync(string id)
        {
            string serverUrl = null;
            FlightPlan flightPlan = GetFromCache(id);
            if (flightPlan != null)
                return flightPlan;
            else
            {
                flightPlan = GetFromDb(id);
            }
            if (flightPlan != null)
                return flightPlan;
            else
            {
                lock (balanceLock)
                {
                    serverUrl = (string)scheduledCache.TryGetValue(id);
                }
            }
            if (serverUrl == null)
                throw new Exception("ID: " + id + " was not found");
            try
            {
                flightPlan = await GetFlightPlanFromServer(serverUrl, id);
            }catch(Exception e)
            {
                throw e;
            }
            return flightPlan;
        }

        private async Task<FlightPlan> GetFlightPlanFromServer(string url, string id)
        {
            FlightPlan retFP = null;
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(url + "/api/FlightPlan/" + id
                , HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw new Exception("Cannot get an answer from: "+url);
            }
            if (response != null && response.IsSuccessStatusCode)
            {
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new CustomContractResolver();
                // Get the response
                var customerJsonString = await response.Content.ReadAsStringAsync();
                // Deserialise the data
                retFP = JsonConvert.DeserializeObject<FlightPlan>(custome‌​rJsonString, settings);
            }
            if (retFP == null)
            {
                throw new Exception("There was a problem getting the flight plan with the id: "+id+" from: " + url);
            }
            return retFP;
        }

        private FlightPlan GetFromCache(string id)
        {
            List<Tuple<string, FlightPlan>> cacheList = null;
            lock (balanceLock)
                cacheList = (List<Tuple<string, FlightPlan>>)scheduledCache.TryGetValue("FlightPlan");
            if (cacheList == null)
                return null;
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
                return _tuple.Item2;
            return null;
        }

        private FlightPlan GetFromDb(string id)
        {
            JsonFlightPlan jsFlightPlan;
            lock (balanceLock)
                jsFlightPlan = db.FlightPlanes.Find(id);
            if (jsFlightPlan == null)
                return null;
            string jsString = jsFlightPlan.Json;
            if (jsString == null)
                return null;
            FlightPlan flightPlan=JsonConvert.DeserializeObject<FlightPlan>(jsString);
            return flightPlan;  
        }
    }
}
