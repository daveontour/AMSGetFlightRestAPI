using AMSGetFlights.Model;
using Experimental.System.Messaging;
using Newtonsoft.Json;
using NLog;

namespace AMSGetFlights.Services
{
    /*
     * Class to facilitate the exchange of information between component via event notifivations
     */
    public class EventExchange :IDisposable
    {
        public event Action<AMSFlight>? OnFlightDeleted;
        public event Action<AMSFlight>? OnFlightUpdated;
        public event Action<AMSFlight>? OnFlightInserted;
        public event Action<GetFlightQueryObject>? OnAPIRequestMade;
        public event Action<GetFlightQueryObject>? OnAPIRequestResult;
        public event Action<string>? OnAPIURLRequestMade;
        public event Action<string>? OnMonitorMessage;
        public event Action<string>? OnTopStatus;
        public event Action? OnServerFlightsUpdates;
        public event Action<bool>? OnFlightServiceRunning;
        public event Action<string>? OnConsoleMessage;
        public event Action<List<Subscription>> OnSubscriptionsChanged;
        public event Action? OnSubscriptionSend;
        public event Action<Subscription>? OnSendBacklog;
        public event Action? OnUserAPICallsUpdated;

        private readonly Logger logger = LogManager.GetLogger("consoleLogger");

        public string LastTopMessage { get; set; }

        public EventExchange()
        {
            OnTopStatus += SetTopStatusMessage;
        }

        public void Dispose()
        {
            OnTopStatus -= SetTopStatusMessage;
        }
    

        private void SetTopStatusMessage(string obj)
        {
            LastTopMessage = obj;
        }

        public void UserAPICallsUpdated()
        {
            OnUserAPICallsUpdated?.Invoke();
        }
        public void URLRequestMade(string message)
        {
            OnAPIURLRequestMade?.Invoke(message);
        }

        public void SendBacklog(Subscription sub)
        {
            OnSendBacklog?.Invoke(sub);  
        }
        public void MonitorMessage(string? message)
        {
            OnMonitorMessage?.Invoke(message);
        }
        public void TopStatusMessage(string? message)
        {
            OnTopStatus?.Invoke(message);
        }
        public void SubscriptionSend()
        {
            OnSubscriptionSend?.Invoke();   
        }
        public void APIRequestMade(GetFlightQueryObject query)
        {
            OnAPIRequestMade?.Invoke(query);
        }
        public void APIRequestResult(GetFlightQueryObject query)
        {
            OnAPIRequestResult?.Invoke(query);
        }

        public void FlightUpdated(AMSFlight flt)
        {
            OnFlightUpdated?.Invoke(flt);
        }
        public void FlightInserted(AMSFlight flt)
        {
            OnFlightInserted?.Invoke(flt);
        }

        public void FlightDeleted(AMSFlight flt)
        {
            OnFlightDeleted?.Invoke(flt);
        }

        public void FlightServiceRunning(bool running)
        {
            OnFlightServiceRunning?.Invoke(running);
        }

        public void Log(string result, GetFlightQueryObject? query = null, string? recordsReturned = null, bool info = false, bool warn = false, bool error = false, bool showQuery = false)
        {
            LogEntry lee = new ();
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
            if (showQuery && query != null)MonitorMessage(JsonConvert.SerializeObject(query, Formatting.Indented));

            if (error) logger.Error(JsonConvert.SerializeObject(lee,Formatting.Indented));
            if (warn) logger.Warn(JsonConvert.SerializeObject(lee, Formatting.Indented));
            if (info) logger.Info(JsonConvert.SerializeObject(lee, Formatting.Indented));
        }

        public void SubscriptionsChanged(List<Subscription> subs)
        {
            OnSubscriptionsChanged?.Invoke(subs);
        }
    }
}
