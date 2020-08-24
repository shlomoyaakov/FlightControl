

namespace FlightControlWeb.Models
{
    public interface IScheduledCache
    {
        object TryGetValue(object key);

        void Delete(string id);

        void Set(string key,object obj);

        object GetLock();
    }
}
