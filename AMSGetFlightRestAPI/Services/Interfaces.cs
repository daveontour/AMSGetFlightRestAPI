﻿using AMSGetFlights.Model;


namespace AMSGetFlights.Services;

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
