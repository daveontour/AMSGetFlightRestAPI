using AMSGetFlights.Model;


namespace AMSGetFlights.Services;

/*
 *  Define all the interfaces fro the project for classes where there may be more than one possible implementation 
 */

public interface IFlightRepositoryDataAccessObject
{
    void DeleteRecord(AMSFlight record);
    int GetNumEntries();
    IEnumerable<StoredFlight> GetStoredFlights(GetFlightQueryObject query, string? kind);
    void Prune(int backWindow);
    void Indate(List<AMSFlight> fls);
    void Upsert(List<AMSFlight> fls);
    IEnumerable<string> GetAllSubscriptions();
    void SaveSubsciptions(List<string> subscriptions);
    void ClearFlights();
}
