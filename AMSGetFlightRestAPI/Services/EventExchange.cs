using AMSGetFlights.Model;
using Experimental.System.Messaging;
using Newtonsoft.Json;
using NLog;

namespace AMSGetFlights.Services
{
    public class EventExchange : IEventExchange
    {
        public event Action<AMSFlight> OnFlightDeleted;
        public event Action OnFlightRepositoryUpdated;
        public event Action<AMSFlight> OnFlightUpdatedOrAdded;
        public event Action<GetFlightQueryObject> OnAPIRequestMade;
        public event Action<string> OnAPIURLRequestMade;
        public event Action<string> OnMonitorMessage;
        public event Action OnServerFlightsUpdates;
        public event Action OnServerNoFlightsUpdates;
        public event Action<bool> OnFlightServiceRunning;
        public event Action<string> OnConsoleMessage;

        private readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public void URLRequestMade(string message)
        {
            OnAPIURLRequestMade?.Invoke(message);
        }
        public void MonitorMessage(string message)
        {
            OnMonitorMessage?.Invoke(message);
        }

        public void APIRequestMade(GetFlightQueryObject query)
        {
            OnAPIRequestMade?.Invoke(query);
        }

        public void FlightRepositoryUpdated()
        {
            OnFlightRepositoryUpdated?.Invoke();
        }

        public void FlightUpdatedOrAdded(AMSFlight flt)
        {
            OnFlightUpdatedOrAdded?.Invoke(flt);
        }

        public void FlightDeleted(AMSFlight flt)
        {
            OnFlightDeleted?.Invoke(flt);
        }

        public void FlightServiceRunning(bool running)
        {
            OnFlightServiceRunning?.Invoke(running);
        }

        public void Log(string result, GetFlightQueryObject? query = null, string? recordsReturned = null, bool info = false, bool warn = false, bool error = false)
        {
            LogEntry lee = new LogEntry();
            if (result != null)
            {
                lee.Result = result;
            }
            if (query != null)
            {
                lee.query = query;
            }
            if (int.TryParse(recordsReturned, out int count))
            {
                lee.RecordsReturned = count;
            }
            MonitorMessage(result);

            if (error) logger.Error(JsonConvert.SerializeObject(lee, Newtonsoft.Json.Formatting.Indented));
            if (warn) logger.Warn(JsonConvert.SerializeObject(lee, Newtonsoft.Json.Formatting.Indented));
            if (info) logger.Info(JsonConvert.SerializeObject(lee, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
