using AMSGetFlights.Model;

namespace AMSGetFlights.Services
{
    public interface IAMSGetFlightStatusService
    {
        bool Running { get; set; }
        Task BackgroundProcessing(CancellationToken stoppingToken);
        void PopulateFlightCache();
        Task Start();
        void UpdateFlightCache();
    }
    public interface IFlightRepository
    {
        DateTime MaxDateTime { get; set; }
        DateTime MinDateTime { get; set; }

        event Action<AMSFlight> OnFlightDeleted;
        event Action OnFlightRepositoryUpdated;
        event Action<AMSFlight> OnFlightUpdatedOrAdded;
        void DeleteFlight(AMSFlight flt);
        List<AMSFlight> GetFlights(GetFlightQueryObject query);
        void UpdateOrAddFlight(AMSFlight flt);
        void PruneRepo(int backWindow);
        void BulkUpdateOrInsert(List<AMSFlight> fls);
        int GetNumEntries();
    }
    public interface IFlightRepositoryDataAccessObject
    {
        void DeleteRecord(AMSFlight record);
        int GetNumEntries();
        IEnumerable<StoredFlight> GetStoredFlights(GetFlightQueryObject query, string? kind);
        void Prune(int backWindow);
        void Upsert(List<AMSFlight> fls);
    }
    public interface IFlightRequestHandler
    {
        IFlightRepository repo { get; set; }

        event Action<GetFlightQueryObject> OnAPIRequestMade;
        event Action<string> OnAPIURLRequestMade;
        event Action<string> OnMonitorMessage;
        string CheckQueryStatus(GetFlightQueryObject q);
        List<AMSFlight> GetFlights(GetFlightQueryObject query, bool xml = false);
        List<AMSFlight> GetSingleFlight(string xml, string token);
        List<AMSFlight> GetFlightsFromXML(string xml, GetFlightQueryObject query);
        GetFlightQueryObject GetQueryObject(HttpRequest request, string format);
        void URLRequestMade(string message);
        void MonitorMessage(string message);

    }
    public interface IGetFlightsConfig
    {
        string AdminPass { get; set; }
        List<AirportSource> Airports { get; set; }
        bool AllowAMSXFormat { get; set; }
        bool AllowAnnonymousUsers { get; set; }
        bool AllowJSONFormat { get; set; }
        int BackwardWindowInDays { get; set; }
        int ChunkSizeInDays { get; set; }
        string? ConfigurationFile { get; set; }
        Dictionary<string, string> CustomFieldToParameter { get; set; }
        int ForewardWindowInDays { get; set; }
        Dictionary<string, string> MappedQueryParameters { get; set; }
        string? RefreshCron { get; set; }
        string? StorageDirectory { get; set; }
        Dictionary<string, User> Users { get; set; }
        double UTCOffset { get; set; }

        object Clone();
    }
    public interface IGetFlightsConfigService
    {
        event Action OnConfigLoaded;
        string? CurrentConfigFile { get; set; }
        GetFlightsConfig? config { get; set; }
        void ApplyConfig(GetFlightsConfig? newconfig);
        void SaveConfig(GetFlightsConfig localconfig = null);

    }
}
