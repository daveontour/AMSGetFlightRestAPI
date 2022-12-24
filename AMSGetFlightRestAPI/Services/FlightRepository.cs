using AMSGetFlights.Model;

namespace AMSGetFlights.Services
{
    public class FlightRepository : IFlightRepository
    {
        public event Action<AMSFlight>? OnFlightUpdatedOrAdded;
        public event Action<AMSFlight>? OnFlightDeleted;
        public event Action? OnFlightRepositoryUpdated;

        private IGetFlightsConfigService configService;
        public IFlightRepositoryDataAccessObject flightRepo; 
        
        public DateTime MaxDateTime { get; set; } = DateTime.MaxValue;
        public DateTime MinDateTime { get; set; } = DateTime.MinValue;

        public FlightRepository(IFlightRepositoryDataAccessObject flightRepo, IGetFlightsConfigService configService)
        {
            this.configService = configService;
            this.flightRepo = flightRepo;   
        }
        public void UpdateOrAddFlight(AMSFlight flt)
        {

            if (flt.flightId.scheduleDateTime < MinDateTime || flt.flightId.scheduleDateTime > MaxDateTime)
            {
                return;
            }

            //// See if the flight is already in the database so we know whether to do an Update or insert and to fire the right type of notification
            //StoredFlight fl = flightRepo.GetStoredFlight(flt);

            //if (fl == null)
            //{
            //    flightRepo.SaveRecord(flt);
            //    OnFlightAdded?.Invoke(flt);
            //}
            //else
            //{
            //    flightRepo.UpdateRecord(flt);
            //    OnFlightUpdated?.Invoke(flt);
            //}
            flightRepo.Upsert(new List<AMSFlight>() { flt });
            OnFlightUpdatedOrAdded?.Invoke(flt);
            OnFlightRepositoryUpdated?.Invoke();
        }
        public void DeleteFlight(AMSFlight flt)
        {
            flightRepo.DeleteRecord(flt);
            OnFlightDeleted?.Invoke(flt);
            OnFlightRepositoryUpdated?.Invoke();
        } 
        public void BulkUpdateOrInsert(List<AMSFlight> fls)
        {
            flightRepo.Upsert(fls);
        }
        public List<AMSFlight> GetFlights(GetFlightQueryObject query)
        {

            string? kind = null;

            if (query.type != null)
            {
                if (query.type.ToLower().Contains("dep"))
                {
                    kind = "Departure";
                }
                if (query.type.ToLower().Contains("arr"))
                {
                    kind = "Arrival";
                }
            }

            // Do the query to get the XML for the flight
            List<AMSFlight> fls = new List<AMSFlight>();

            // Construct a list of the Flight objects
            var flights = flightRepo.GetStoredFlights(query, kind);
            if (flights != null)
            {
                foreach (StoredFlight stfl in flights)
                {
                    fls.Add(new AMSFlight(stfl.XML, configService.config, DateTime.Parse(stfl.lastupdate).AddHours(configService.config.UTCOffset).ToString("yyyy-MM-ddTHH:mm:ssK")));
                }
            } else
            {
                return fls;
            }


            //Special handling of additional lookup options
            if (query.route != null)
            {
                fls = fls.Where(f => CheckRoute(f, query.route, "airportIATA") == true).ToList();
            }
            if (query.routeICAO != null)
            {
                fls = fls.Where(f => CheckRoute(f, query.route, "airportICAO") == true).ToList();
            }

            bool fDeleted = true;
            if (query.includeDeletedFlights.ToLower() == "false")
            {
                fDeleted = false;
            }

            //Extra Dynamic Custom Field queries
            Dictionary<string, string> allowedExtras = configService.config.GetMappedExtras();


            foreach (string xtra in query.queryParams.Keys)
            {
                if (allowedExtras.ContainsKey(xtra))
                {
                    fls = fls.Where(f => CheckCustomField(f, allowedExtras[xtra], query.queryParams[xtra]) == true).ToList();
                }
            }

            //Return the filtered values
            return fls;
        }
        public void PruneRepo(int backWindow)
        {
            flightRepo.Prune(backWindow);
        }
        public int GetNumEntries()
        {
           return flightRepo.GetNumEntries();
        }
        private bool CheckRoute(AMSFlight f, string route, string codeSet)
        {
            foreach (Dictionary<string, string> r in f.route)
            {
                try
                {
                    if (r[codeSet] == route)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return false;
        }
        private bool CheckCustomField(AMSFlight f, string ExternalName, string value)
        {
            try
            {
                if (f.Values == null) return false;
                foreach (string cf in f.Values.Keys)
                {
                    try
                    {
                        if (cf == ExternalName && f.Values[cf].ToLower() == value.ToLower())
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw ex;
                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

            return false;
        }
    }
}
