using Microsoft.Extensions.Caching.Memory;

namespace FlightControlWeb.Models
{
    public class ScheduledCache : IScheduledCache
    {
        private IMemoryCache _cache;
        private readonly object balanceLock = new object();
        public ScheduledCache(IMemoryCache cache)
        {
            this._cache = cache;
        }
        public void Delete(string id)
        {
            _cache.Remove(id);
        }
        public void Set(string key, object obj)
        {
            _cache.Set(key, obj);
        }
        public object TryGetValue(object key)
        {
            object value;
            _cache.TryGetValue(key, out value);
            return value;
        }

        public object GetLock()
        {
            return balanceLock;
        }
    }
}
