using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace FlightControlWeb.Models
{
    public class ServerManager : IServerManager
    {
        IScheduledCache scheduledCache;
        private readonly object balanceLock = new object();
        private SQLiteDBContext db = new SQLiteDBContext();
        public ServerManager (IScheduledCache sc){
            scheduledCache = sc;
            List<Server> list = new List<Server>();
            sc.Set("Server", list);
        }

        /*
         * Add server to the server.
         * If the server is invalid or the server id is already exist then an exception is being thrown.
         */
        public void AddServer(Server server)
        {
            //check if the server is valid.
            if (server.ServerId == null || server.ServerUrl == null)
            {
                throw new Exception("Invalid server");
            }
            string id = server.ServerId;
            lock(balanceLock){
                if (scheduledCache.TryGetValue(id) != null || db.Servers.Find(id) != null)
                {
                    throw new Exception("ID: " + id + " is already exists");
                }
            }
            SaveServer(server);
        }

        /*
         * Delete the server from the database and from the cache if exists.
         * if the server was not found then exception is thrwon.
         */ 
        public void DeleteServer(string id)
        {
            DeleteServerFromCache(id);
            try
            {
                DeleteServerFromDb(id);
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        /*
         * Delete from the data base the server with the spcified id.
         */
        private void DeleteServerFromDb(string id)
        {
            try
            {
                lock (balanceLock)
                {
                    db.Servers.Remove(db.Servers.Find(id));
                    db.SaveChanges();
                }
            }
            catch
            {
                throw new Exception("ID: "+id+" doesn't exist");
            }
            
        }

        /*
         * Delete from the cache the server with the specified id, and remove the link between server and
         * and flight object if exist.
         */
        private void DeleteServerFromCache(string id)
        {
            string flightId = null;
            lock(balanceLock)
             flightId = (string)scheduledCache.TryGetValue(id);
            if (flightId != null)
            {
                //remove the link between flight object and the server.
                lock (balanceLock)
                {
                    scheduledCache.Delete(flightId);
                    scheduledCache.Delete(id);
                }
            }
            List<Server> cacheList = null;
            lock (balanceLock)
                cacheList = (List<Server>)scheduledCache.TryGetValue("Server");
            if (cacheList == null)
                return;
            List<Server> list = new List<Server>();
            lock (balanceLock)
                list.AddRange(cacheList);
            Server server = null;
            //delete from the cache the server.
            foreach (Server _server in list)
            {
                if (_server.ServerId.Equals(id))
                {
                    server = _server;
                    break;
                }
            }
            if (server != null)
            {
                lock (balanceLock)
                    cacheList.Remove(server);
            }
        }
        //Gets the list of remote servers.
        public List<Server> GetServers()
        {
            List<Server> list;
            lock (balanceLock)
                list = db.Servers.ToList();
            if (list == null)
            {
                return new List<Server>();
            }
            return list;
        }

        /*
         * Save the server in the cache and the data base.
         */
        public void SaveServer(Server server)
        {
            List<Server> list = null;
            lock (balanceLock)
                list = (List<Server>)scheduledCache.TryGetValue("Server");

            lock (balanceLock)
            {
                if (list == null)
                    list.Add(server);
                db.Servers.Add(server);
                db.SaveChanges();
            }
        }
    }
}
