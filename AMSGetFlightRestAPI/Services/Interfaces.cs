using AMSGetFlights.Model;
using AMSGetFlights.Services;

namespace AMSGetFlights.Services
{
    //public interface IEventExchange
    //{
    //    event Action<AMSFlight> OnFlightDeleted;
    //    event Action OnFlightRepositoryUpdated;
    //    event Action<AMSFlight> OnFlightUpdatedOrAdded;
    //    event Action<GetFlightQueryObject> OnAPIRequestMade;
    //    event Action<string> OnAPIURLRequestMade;
    //    event Action<string> OnMonitorMessage;

    //    event Action OnServerFlightsUpdates;
    //    event Action OnServerNoFlightsUpdates;
    //    event Action<bool> OnFlightServiceRunning;
    //    event Action<string> OnConsoleMessage;

    //    void URLRequestMade(string message);
    //    void APIRequestMade(GetFlightQueryObject query);
    //    void MonitorMessage(string message);
    //    void FlightRepositoryUpdated();
    //    void FlightUpdatedOrAdded(AMSFlight flt);
    //    void FlightDeleted(AMSFlight flt);
    //    void FlightServiceRunning(bool running);
    //    void Log(string result, GetFlightQueryObject? query = null, string? recordsReturned = null, bool info = false, bool warn = false, bool error = false, bool showQuery = false);
    //}


    //public interface IFlightRepository
    //{
    //    DateTime MaxDateTime { get; set; }
    //    DateTime MinDateTime { get; set; }
    //    void DeleteFlight(AMSFlight flt);
    //    List<AMSFlight> GetFlights(GetFlightQueryObject query);
    //    void UpdateOrAddFlight(AMSFlight flt);
    //    void PruneRepo(int backWindow);
    //    void BulkUpdateOrInsert(List<AMSFlight> fls);
    //    int GetNumEntries();
    //    List<Subscription> GetAllSubscriptions();
    //    void SaveSubsciptions(List<Subscription> subscriptions);

    //}
    public interface IFlightRepositoryDataAccessObject
    {
        void DeleteRecord(AMSFlight record);
        int GetNumEntries();
        IEnumerable<StoredFlight> GetStoredFlights(GetFlightQueryObject query, string? kind);
        void Prune(int backWindow);
        void Upsert(List<AMSFlight> fls);
        IEnumerable<string> GetAllSubscriptions();
        void SaveSubsciptions(List<string> subscriptions);
        void ClearFlights();
    }
    //public interface IFlightRequestHandler
    //{
    //    string CheckQueryStatus(GetFlightQueryObject q);
    //    List<AMSFlight> GetFlights(GetFlightQueryObject query, bool IsXml = false);
    //    List<AMSFlight> GetSingleFlight(string xml, string token, bool IsXml = false);
    //    List<AMSFlight> GetFlightsFromXML(string xml, GetFlightQueryObject query, bool IsXml = false);
    //    GetFlightQueryObject GetQueryObject(HttpRequest request, string format);

    //}
    //public interface IGetFlightsConfig
    //{
    //    string AdminPass { get; set; }
    //    List<AirportSource> Airports { get; set; }
    //    bool AllowAMSXFormat { get; set; }
    //    bool AllowAnnonymousUsers { get; set; }
    //    bool AllowJSONFormat { get; set; }
    //    int BackwardWindowInDays { get; set; }
    //    int ChunkSizeInDays { get; set; }
    //    string? ConfigurationFile { get; set; }
    //    Dictionary<string, string> CustomFieldToParameter { get; set; }
    //    int ForewardWindowInDays { get; set; }
    //    Dictionary<string, string> MappedQueryParameters { get; set; }
    //    string? RefreshCron { get; set; }
    //    string? StorageDirectory { get; set; }
    //    Dictionary<string, User> Users { get; set; }
    //    double UTCOffset { get; set; }

    //    object Clone();
    //}
    //public interface IGetFlightsConfigService
    //{
    //    string? CurrentConfigFile { get; set; }
    //    GetFlightsConfig config { get; set; }
    //    void ApplyConfig(GetFlightsConfig? newconfig);
    //    void SaveConfig(GetFlightsConfig localconfig = null);

    //}
}
