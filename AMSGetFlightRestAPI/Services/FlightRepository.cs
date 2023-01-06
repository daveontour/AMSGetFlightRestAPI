using AMSGetFlights.Model;
using Newtonsoft.Json;

namespace AMSGetFlights.Services
{
    public class FlightRepository 
    {
        private readonly GetFlightsConfigService configService;
        private readonly IFlightRepositoryDataAccessObject flightRepo;
        private readonly EventExchange eventExchange;

        public DateTime MaxDateTime { get; set; } = DateTime.MaxValue;
        public DateTime MinDateTime { get; set; } = DateTime.MinValue;

        public FlightRepository(IFlightRepositoryDataAccessObject flightRepo, GetFlightsConfigService configService, EventExchange eventExchange)
        {
            this.configService = configService;
            this.flightRepo = flightRepo;
            this.eventExchange = eventExchange;
        }
        public void UpdateOrAddFlight(AMSFlight flt)
        {

            if (flt.flightId.scheduleDateTime < MinDateTime || flt.flightId.scheduleDateTime > MaxDateTime)
            {
                return;
            }

            flightRepo.Upsert(new List<AMSFlight>() { flt });
            eventExchange.FlightUpdatedOrAdded(flt);
            eventExchange.FlightRepositoryUpdated();
        }
        public void DeleteFlight(AMSFlight flt)
        {
            flightRepo.DeleteRecord(flt);
            eventExchange.FlightDeleted(flt);
            eventExchange.FlightRepositoryUpdated();
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
                    DateTime lastUpdate = DateTime.MinValue;
                    try
                    {
                        lastUpdate = DateTime.Parse(stfl.Lastupdate);   
                    } catch (Exception)
                    {
                        lastUpdate = DateTime.MinValue;
                    }
                    if(lastUpdate >= query.updatedFrom) 
                    fls.Add(new AMSFlight(stfl.XML, configService.config, DateTime.Parse(stfl.Lastupdate).AddHours(configService.config.UTCOffset).ToString("yyyy-MM-ddTHH:mm:ssK")));
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
            if (allowedExtras.ContainsKey("callsign"))
            {
                allowedExtras.Remove("callsign");
            }

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

        public void ClearFlights()
        {
            flightRepo.ClearFlights();
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

        public List<Subscription> GetAllSubscriptions()
        {
            List<Subscription> subscriptions = new List<Subscription>();
            try
            {
                List<string> subtrs = flightRepo.GetAllSubscriptions().ToList();

                foreach (string subtr in subtrs)
                {
                    Subscription subscription = JsonConvert.DeserializeObject<Subscription>(subtr);
                    subscriptions.Add(subscription);
                }
                return subscriptions;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return subscriptions;
            }
        }

        public void SaveSubsciptions(List<Subscription> subscriptions)
        {
            List<string> substr = new List<string>();
            try
            {
                foreach (Subscription s in subscriptions)
                {
                   string ss = JsonConvert.SerializeObject(s);
                    substr.Add(ss);
                }
                flightRepo.SaveSubsciptions(substr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }
    }
}
