using AMSGetFlights.Model;
using Microsoft.Extensions.Primitives;

namespace AMSGetFlights.Services
{
    public class FlightRequestHandler : IFlightRequestHandler
    {
        public event Action<GetFlightQueryObject> OnAPIRequestMade;
        public event Action<string> OnAPIURLRequestMade;
        public event Action<string> OnMonitorMessage;
        public IFlightRepository repo { get; set; }
        private IGetFlightsConfigService configService;

        public FlightRequestHandler(IFlightRepository repo, IGetFlightsConfigService configService)
        {
            this.repo = repo;
            this.configService = configService;
        }
        public List<AMSFlight> GetFlights(GetFlightQueryObject query, bool xml = false)
        {

            // Get the list of flights
            var res = repo.GetFlights(query);

            // Adjust the result so only elements the user is allowed to see are set
            List<string> validFields = configService.config.ValidUserFields(query.token);
            List<string> validCustomFields = configService.config.ValidUserCustomFields(query.token);

            foreach (var flight in res)
            {
                foreach (var prop in flight.GetType().GetProperties())
                {
                    if (prop.Name != "flightId" && prop.Name != "Key" && !validFields.Contains(prop.Name))
                    {
                        if (prop.Name == "XmlRaw" && xml)
                        {
                            continue;
                        }
                        prop.SetValue(flight, null);
                    }
                }
                if (flight.Values != null && validCustomFields.Count() > 0)
                {
                    Dictionary<string, string> fields = new Dictionary<string, string>();
                    foreach (string key in flight.Values.Keys)
                    {
                        if (validCustomFields.Contains(key))
                        {
                            fields.Add(key, flight.Values[key]);
                        }
                    }
                    flight.Values = fields;
                    // flight.Values = flight.Values.Where(f => validCustomFields.Contains(f.name)).ToList();
                }
            }

            OnAPIRequestMade?.Invoke(query);

            return res;
        }

        // Test the timeframe of the query against what is in the cache
        public string CheckQueryStatus(GetFlightQueryObject q)
        {
            if (q.startQuery >= repo.MinDateTime
                && q.startQuery <= repo.MaxDateTime
                && q.endQuery >= repo.MinDateTime
                && q.endQuery <= repo.MaxDateTime
                ) return "OK";

            if (!(q.startQuery >= repo.MinDateTime
                && q.startQuery <= repo.MaxDateTime)
                &&
                q.endQuery >= repo.MinDateTime
                && q.endQuery <= repo.MaxDateTime
                 ) return "PARTIAL";

            if (q.startQuery >= repo.MinDateTime
                && q.startQuery <= repo.MaxDateTime
                && !(
                q.endQuery >= repo.MinDateTime
                && q.endQuery <= repo.MaxDateTime)
                 ) return "PARTIAL";

            return "OUTOFBOUND";
        }

        // Put all the non core quest parameters into a Dictionary
        public GetFlightQueryObject GetQueryObject(HttpRequest request, string format)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string key in request.Query.Keys)
            {
                StringValues val;
                request.Query.TryGetValue(key, out val);
                dict.Add(key.ToLower(), val.ElementAt(0));
            }

            //
            StringValues values;
            request.Headers.TryGetValue("Authorization", out values);
            string providedUser = "default";
            try
            {
                providedUser = values.ElementAt(0);
                providedUser = providedUser.Replace("Bearer", "").Trim();
            }
            catch (Exception)
            {
                providedUser = "default";
            }

            GetFlightQueryObject query = new GetFlightQueryObject() { queryParams = dict, token = providedUser };
            string user = query.token;
            query.SetDefaults(configService.config.Users[user].Defaults, configService.config.Users[user].Overrides);

            query.Format = format;
            return query;
        }

        public List<AMSFlight> GetSingleFlight(string xml, string token)
        {
            AMSFlight flight = new AMSFlight(xml, configService.config);


            // Adsjust the result so only elements the user is allowed to see are set
            List<string> validFields = configService.config.ValidUserFields(token);
            List<string> validCustomFields = configService.config.ValidUserCustomFields(token);

            foreach (var prop in flight.GetType().GetProperties())
            {
                if (prop.Name != "flightId" && prop.Name != "Key" && !validFields.Contains(prop.Name))
                {
                    if (prop.Name == "XmlRaw")
                    {
                        continue;
                    }
                    prop.SetValue(flight, null);
                }
            }
            if (flight.Values != null && validCustomFields.Count() > 0)
            {
                Dictionary<string, string> fields = new Dictionary<string, string>();
                foreach (string key in flight.Values.Keys)
                {
                    if (validCustomFields.Contains(key))
                    {
                        fields.Add(key, flight.Values[key]);
                    }
                }
                flight.Values = fields;
                // flight.Values = flight.Values.Where(f => validCustomFields.Contains(f.name)).ToList();
            }


            List<AMSFlight> res = new List<AMSFlight>() { flight };

            return res;
        }

        public void URLRequestMade(string message)
        {
            OnAPIURLRequestMade?.Invoke(message);
        }
        public void MonitorMessage(string message)
        {
            OnMonitorMessage?.Invoke(message);
        }
    }
}
