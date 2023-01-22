using AMSGetFlights.Model;
using Microsoft.Extensions.Primitives;
using System.Xml;

namespace AMSGetFlights.Services
{
    public class FlightRequestHandler
    {
        private readonly FlightRepository repo;
        private readonly GetFlightsConfigService configService;
        private readonly EventExchange eventExchange;
        private readonly FlightSanitizer sanitizer;

        public FlightRequestHandler(FlightRepository repo, GetFlightsConfigService configService, EventExchange eventExchange, FlightSanitizer sanitizer)
        {
            this.repo = repo;
            this.configService = configService;
            this.eventExchange = eventExchange;
            this.sanitizer = sanitizer;
        }
        public List<AMSFlight> GetFlights(GetFlightQueryObject query, bool IsXML = false)
        {
           
            // Get the list of flights
            var res = repo.GetFlights(query);

            var result = sanitizer.SanitizeFlights(res, IsXML, query.token);

            eventExchange.APIRequestMade(query);

            return result;
        }

        public List<FlightIDExtended> GetFlightSchedule(GetFlightQueryObject query, bool IsXML = false)
        {

            // Get the list of flights
            var res = repo.GetFlights(query);

            List<FlightIDExtended> flights = new List<FlightIDExtended>();
            foreach (var flight in res)
            {
                FlightIDExtended fe = new FlightIDExtended(flight.flightId, flight.route);
                flights.Add(fe);
            }

            eventExchange.APIRequestMade(query);

            return flights;
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

        // Put all the non core request parameters into a Dictionary
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

        public List<AMSFlight> GetFlightsFromXML(string xml, GetFlightQueryObject query, bool IsXML = false)
        {
            XmlDocument doc = new();
            doc.LoadXml(xml);

            // Get the list of flights           
            List<AMSFlight> flights = new List<AMSFlight>();
            foreach (XmlNode f in doc.SelectNodes(".//*[local-name() = 'Flights']/*[local-name() = 'Flight']"))
            {
                AMSFlight flight = new(f, configService.config);
                flights.Add(flight);
            }

            flights = sanitizer.SanitizeFlights(flights, IsXML, query.token);

            return flights;
        }

        public List<AMSFlight> GetSingleFlight(string xml, string token, bool IsXML = false)
        {
            AMSFlight flight = new(xml, configService.config);
            flight = sanitizer.SanitizeFlight(flight, IsXML, token);

            List<AMSFlight> res = new() { flight };

            return res;
        }
    }
}
