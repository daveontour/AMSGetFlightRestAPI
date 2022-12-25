using AMSGetFlights.Model;
using Microsoft.Extensions.Primitives;
using System.Xml;

namespace AMSGetFlights.Services
{
    public class FlightRequestHandler : IFlightRequestHandler
    {
        public IFlightRepository repo { get; set; }
        private IGetFlightsConfigService configService;
        private IEventExchange eventExchange;

        public FlightRequestHandler(IFlightRepository repo, IGetFlightsConfigService configService,IEventExchange eventExchange)
        {
            this.repo = repo;
            this.configService = configService;
            this.eventExchange = eventExchange;
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

            eventExchange.APIRequestMade(query);

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
            Dictionary<string, string> dict = new();
            foreach (string key in request.Query.Keys)
            {
                request.Query.TryGetValue(key, out StringValues val);
                dict.Add(key.ToLower(), val.ElementAt(0));
            }

            //
            request.Headers.TryGetValue("Authorization", out StringValues values);

            string providedUser;
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

        public List<AMSFlight> GetFlightsFromXML(string xml, GetFlightQueryObject query)
        {
            XmlDocument doc = new();
            doc.LoadXml(xml);

            // Get the list of flights           
            List<AMSFlight> flights = new List<AMSFlight>();
            foreach(XmlNode f in doc.SelectNodes(".//Flight"))
            {
                AMSFlight flight = new(f, configService.config)
                {
                    XmlRaw = f.OuterXml
                };
                flights.Add(flight);
            }

            // Adjust the result so only elements the user is allowed to see are set
            List<string> validFields = configService.config.ValidUserFields(query.token);
            List<string> validCustomFields = configService.config.ValidUserCustomFields(query.token);

            foreach (var flight in flights)
            {
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
                if (flight.Values != null && validCustomFields.Count > 0)
                {
                    Dictionary<string, string> fields = new();
                    foreach (string key in flight.Values.Keys)
                    {
                        if (validCustomFields.Contains(key))
                        {
                            fields.Add(key, flight.Values[key]);
                        }
                    }
                    flight.Values = fields;
                }
            }

            return flights;
        }

        public List<AMSFlight> GetSingleFlight(string xml, string token)
        {
            AMSFlight flight = new(xml, configService.config);


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
            if (flight.Values != null && validCustomFields.Count > 0)
            {
                Dictionary<string, string> fields = new();
                foreach (string key in flight.Values.Keys)
                {
                    if (validCustomFields.Contains(key))
                    {
                        fields.Add(key, flight.Values[key]);
                    }
                }
                flight.Values = fields;
            }


            List<AMSFlight> res = new() { flight };

            return res;
        }
    }
}
