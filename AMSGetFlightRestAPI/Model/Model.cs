using AMSGetFlights.Services;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Xml;

namespace AMSGetFlights.Model
{
    public class AirportSource : ICloneable
    {
        public string? AptCode { get; set; }
        public string? Token { get; set; }
        public string? WSURL { get; set; }
        public string? NotificationQueue { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public class User : ICloneable
    {
        public string Token { get; set; }
        public string? Name { get; set; }
        public bool Enabled { get; set; } = false;
        public bool AllowXML { get; set; } = false;
        public List<string> AllowedAirports { get; set; } = new List<string>();
        public List<string> AllowedFields { get; set; } = new List<string>();
        public List<string> AllowedCustomFields { get; set; } = new List<string>();
        public Dictionary<string, string> Defaults { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Overrides { get; set; } = new Dictionary<string, string>();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public class ServerStatus
    {
        public string EarliestEntry { get; set; }
        public string LatestEntry { get; set; }
        public int NumberOfEntries { get; set; }
        public long ProcessMemory { get; set; }
        public string Error { get; set; }
    }
    public class GetFlightsResponse
    {
        public GetFlightQueryObject query { get; set; }
        public bool partialResutlsRetuned { get; set; } = false;
        public string error { get; set; }
        public List<AMSFlight> flights { get; set; }
    }
    public class LogEntry
    {
        public GetFlightQueryObject query { get; set; }
        public string Result { get; set; }
        public int RecordsReturned { get; set; } = 0;
    }
  
    public partial class AMSFlight : ICloneable
    {
        public AMSFlight()
        {

        }

        public bool HasUserInterestedChanges(Subscription sub )
        {
            // No filters are set, so pass the flight
            if( !sub.ChangeEstimated 
                && !sub.ChangeResourceBaggageReclaim 
                && !sub.ChangeResourceCheckIn 
                && !sub.ChangeResourceGate 
                && !sub.ChangeResourceStand)
            {
                return true;
            }

            if (sub.ChangeEstimated)
            {
                if (flightId.flightkind.ToLower().StartsWith("arr"))
                {
                    if(XmlRaw.Contains("<Change propertyName=\"de-G_MostConfidentArrivalTime\">")) return true;
                }
                if (flightId.flightkind.ToLower().StartsWith("dep"))
                {
                    if (XmlRaw.Contains("<Change propertyName=\"de-G_MostConfidentDepartureTime\">")) return true;
                }
            }
            if (sub.ChangeResourceStand)
            {
                if (XmlRaw.Contains("<StandSlotsChange>")) return true;
            } 
            if (sub.ChangeResourceCheckIn)
            {
                if (XmlRaw.Contains("<CheckInSlotsChange>")) return true;
            } 
            if (sub.ChangeResourceGate)
            {
                if (XmlRaw.Contains("<GateSlotsChange>")) return true;
            } 
            if (sub.ChangeResourceBaggageReclaim)
            {
                if (XmlRaw.Contains("<CarouselSlotsChange>")) return true;
            }

            return false;
        }

        public AMSFlight(string xml, GetFlightsConfig config)
        {
            XmlDocument doc = new XmlDocument();
            xml = xml.Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-datatypes\"", "")
         .Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-messages\"", "")
         .Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-webservice\"", "");
            doc.LoadXml(xml);
            XmlNode node = doc.SelectSingleNode("//Flight");
            ConfigFlight(node, config);
            XmlRaw = node.OuterXml;
            LastUpdated = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK");
        }

        public AMSFlight(string xml, GetFlightsConfig config, string timestamp = null)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.SelectSingleNode("//Flight");
            ConfigFlight(node, config);
            XmlRaw = xml;
            LastUpdated = DateTime.Parse(timestamp).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK");
        }
        public AMSFlight(XmlDocument doc, GetFlightsConfig config)
        {
            XmlNode node = doc.SelectSingleNode("//*[local-name() = 'Flight']");
            ConfigFlight(node, config);
            XmlRaw = node.OuterXml;
        }
        public AMSFlight(XmlNode node, GetFlightsConfig config)
        {

            ConfigFlight(node, config);
            XmlRaw = node.OuterXml;
        }
        public void ConfigFlight(XmlNode node, GetFlightsConfig config)
        {
            if (node == null)
            {
                Console.WriteLine("Error");
            }
            string nature = GetValue(".//FlightId/FlightKind", node);
            string airlineIata = GetValue(".//AirlineDesignator[@codeContext = 'IATA']", node);
            string airlineIcao = GetValue(".//AirlineDesignator[@codeContext='ICAO']", node);
            string fltNum = GetValue(".//FlightId/FlightNumber", node);
            string STD = GetValue(".//FlightId/ScheduledDate", node);
            string apIATA = GetValue(".//FlightId/AirportCode[@codeContext='IATA']", node);
            string apICAO = GetValue(".//FlightId/AirportCode[@codeContext='ICAO']", node);
            string flightUniqueID = GetValue(".//FlightState/Value[@propertyName='FlightUniqueID']", node);
            string STO = GetValue(".//FlightState/ScheduledTime", node);


            string lnature = GetValue(".//FlightState/LinkedFlight/FlightId/FlightKind", node);
            string lairlineIata = GetValue(".//FlightState/LinkedFlight/FlightId/AirlineDesignator[@codeContext = 'IATA']", node);
            string lairlineIcao = GetValue(".//FlightState/LinkedFlight/FlightId/AirlineDesignator[@codeContext='ICAO']", node);
            string lfltNum = GetValue(".//FlightState/LinkedFlight/FlightId/FlightNumber", node);
            string lSTD = GetValue(".//FlightState/LinkedFlight/FlightId/ScheduledDate", node);
            string lapIATA = GetValue(".//FlightState/LinkedFlight/FlightId/AirportCode[@codeContext='IATA']", node);
            string lapICAO = GetValue(".//FlightState/LinkedFlight/FlightId/AirportCode[@codeContext='ICAO']", node);
            string lflightUniqueID = GetValue(".//FlightState/LinkedFlight/Value[@propertyName='FlightUniqueID']", node);
            string lSTO = GetValue(".//FlightState/LinkedFlight/Value[@propertyName='ScheduledTime']", node);

            FlightID fl = new FlightID();
            fl.flightNumber = fltNum;
            fl.iataAirline = airlineIata;
            fl.icaoAirline = airlineIcao;
            fl.iatalocalairport = apIATA;
            fl.icaolocalairport = apICAO;
            fl.scheduleDate = STD;
            fl.scheduleTime = STO;
            fl.flightkind = nature;
            fl.flightUniqueID = flightUniqueID;
            fl.flightID = flightUniqueID;
            this.flightId = fl;

            FlightID lfl = new FlightID();
            lfl.flightNumber = lfltNum;
            lfl.iataAirline = lairlineIata;
            lfl.icaoAirline = lairlineIcao;
            lfl.iatalocalairport = lapIATA;
            lfl.icaolocalairport = lapICAO;
            lfl.scheduleDate = lSTD;
            lfl.scheduleTime = lSTO;
            lfl.flightkind = lnature;
            lfl.flightUniqueID = lflightUniqueID;
            lfl.flightID = lflightUniqueID;

            this.linkedflightId = lfl;

            aircrafttypeicao = GetValue(".//FlightState/AircraftType/AircraftTypeId/AircraftTypeCode[@codeContext='ICAO']", node);
            aircrafttypeiata = GetValue(".//FlightState/AircraftType/AircraftTypeId/AircraftTypeCode[@codeContext='IATA']", node);

            //Custom Field Processing
            Values.Add("callsign", $"{airlineIata}{fltNum}");
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/Value"))
            {
                string name = xmlNode.Attributes["propertyName"].Value;
                if (config.CustomFieldToParameter.ContainsKey(name))
                {
                    name = config.CustomFieldToParameter[name];
                }
                string value = xmlNode.InnerText;
                // Values.Add(new CustomField(name, value));
                try
                {
                    Values.Add(name, value);
                }
                catch (Exception)
                {
                    Values[name] = value;
                }
            }

            //Custom Table Processing
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/TableValue"))
            {
                string name = xmlNode.Attributes["propertyName"]?.Value;
                List<Dictionary<string, string>> table = new List<Dictionary<string, string>>();
                foreach (XmlNode row in xmlNode.SelectNodes("./Row"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    foreach (XmlNode e in row.SelectNodes("./Value"))
                    {
                        string prop = e.Attributes["propertyName"]?.Value;
                        string value = e.InnerText;
                        dict.Add(prop, value);
                    }
                    table.Add(dict);
                }
                CustomTables.Add(name, table);
            }

            //Code Share
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/CodeShares"))
            {
                foreach (XmlNode row in xmlNode.SelectNodes("./CodeShare"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string airlineIATA = row.SelectSingleNode("./AirlineDesignator[@codeContext='IATA']").InnerText;
                    string airlineICAO = row.SelectSingleNode("./AirlineDesignator[@codeContext='ICAO']").InnerText;
                    string flightNumber = row.SelectSingleNode("FlightNumber").InnerText;

                    dict.Add("airlineIATA", airlineIATA);
                    dict.Add("airlineICAO", airlineICAO);
                    dict.Add("flightNumber", flightNumber);

                    codeShares.Add(dict);
                }
            }
            //Stand Slots
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/StandSlots"))
            {
                foreach (XmlNode row in xmlNode.SelectNodes("./StandSlot"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string start = row.SelectSingleNode("./Value[@propertyName='StartTime']").InnerText;
                    string end = row.SelectSingleNode("./Value[@propertyName='EndTime']").InnerText;
                    string cat = row.SelectSingleNode("./Value[@propertyName='Category']")?.InnerText;
                    string stand = row.SelectSingleNode("./Stand/Value[@propertyName='Name']")?.InnerText;
                    string area = row.SelectSingleNode("./Stand/Area/Value[@propertyName='Name']")?.InnerText;

                    dict.Add("StartTime", start);
                    dict.Add("EndTime", end);
                    if (cat != null) dict.Add("Category", cat);
                    if (stand != null) dict.Add("Stand", stand);
                    if (area != null) dict.Add("Area", area);

                    StandSlots.Add(dict);
                }
            }

            //Carousel Slots
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/CarouselSlots"))
            {
                foreach (XmlNode row in xmlNode.SelectNodes("./CarouselSlot"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string start = row.SelectSingleNode("./Value[@propertyName='StartTime']").InnerText;
                    string end = row.SelectSingleNode("./Value[@propertyName='EndTime']").InnerText;
                    string cat = row.SelectSingleNode("./Value[@propertyName='Category']")?.InnerText;
                    string name = row.SelectSingleNode("./Carousel/Value[@propertyName='Name']")?.InnerText;
                    string area = row.SelectSingleNode("./Carousel/Area/Value[@propertyName='Name']")?.InnerText;

                    dict.Add("StartTime", start);
                    dict.Add("EndTime", end);
                    if (name != null) dict.Add("Carousel", name);
                    if (area != null) dict.Add("Area", area);
                    if (cat != null) dict.Add("Category", cat);

                    CarouselSlots.Add(dict);
                }
            }
            //Gate Slots
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/GateSlots"))
            {
                foreach (XmlNode row in xmlNode.SelectNodes("./GateSlot"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string start = row.SelectSingleNode("./Value[@propertyName='StartTime']").InnerText;
                    string end = row.SelectSingleNode("./Value[@propertyName='EndTime']").InnerText;
                    string cat = row.SelectSingleNode("./Value[@propertyName='Category']")?.InnerText;
                    string name = row.SelectSingleNode("./Gate/Value[@propertyName='Name']")?.InnerText;
                    string area = row.SelectSingleNode("./Gate/Area/Value[@propertyName='Name']")?.InnerText;

                    dict.Add("StartTime", start);
                    dict.Add("EndTime", end);
                    if (cat != null) dict.Add("Category", cat);
                    if (name != null) dict.Add("Gate", name);
                    if (area != null) dict.Add("Area", area);

                    GateSlots.Add(dict);
                }
            }
            //Route
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/Route/ViaPoints"))
            {
                foreach (XmlNode row in xmlNode.SelectNodes("./RouteViaPoint"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string airportIATA = row.SelectSingleNode("./AirportCode[@codeContext='IATA']")?.InnerText;
                    string airportICAO = row.SelectSingleNode("./AirportCode[@codeContext='ICAO']")?.InnerText;
                    string sequnceNumber = row.Attributes["sequenceNumber"]?.Value;

                    dict.Add("airportIATA", airportIATA);
                    dict.Add("airportICAO", airportICAO);
                    dict.Add("sequnceNumber", sequnceNumber);

                    route.Add(dict);
                }
            }
            //Checkin Slots
            foreach (XmlNode xmlNode in node.SelectNodes(".//FlightState/CheckInSlots"))
            {
                foreach (XmlNode row in xmlNode.SelectNodes("./CheckInSlot"))
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string start = row.SelectSingleNode("./Value[@propertyName='StartTime']").InnerText;
                    string end = row.SelectSingleNode("./Value[@propertyName='EndTime']").InnerText;
                    string cat = row.SelectSingleNode("./Value[@propertyName='Category']")?.InnerText;
                    string checkin = row.SelectSingleNode("./CheckIn/Value[@propertyName='Name']")?.InnerText;
                    string area = row.SelectSingleNode("./Area/Value[@propertyName='Name']")?.InnerText;

                    dict.Add("StartTime", start);
                    dict.Add("EndTime", end);
                    if (cat != null) dict.Add("Category", cat);
                    if (checkin != null) dict.Add("CheckIn", checkin);
                    if (area != null) dict.Add("Area", area);

                    CheckInSlots.Add(dict);
                }
            }
        }
        private string GetValue(string xpath, XmlNode node, XmlNamespaceManager nsmgr)
        {
            string value = node.SelectSingleNode(xpath)?.InnerText;
            return value;
        }
        private string GetValue(string xpath, XmlNode node)
        {
            string value = node.SelectSingleNode(xpath)?.InnerText;
            return value;
        }

        [JsonIgnore]
        public string Key
        {
            get
            {
                return flightId.flightID;
            }
            set
            {
                Key = value;
            }
        }
        public FlightID flightId { get; set; }
        public string? callsign
        {
            get
            {
                try
                {
                    if (Values.ContainsKey("callsign"))
                    {
                        return Values["callsign"];
                    }
                    else
                    {
                        return $"{flightId.iataAirline}{flightId.flightNumber}";
                    }
                } catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return $"{flightId.iataAirline}{flightId.flightNumber}";
                }
            }
        }
        public string domesticintcode { get; set; }
        public string aircrafttypeicao { get; set; }
        public string aircrafttypeiata { get; set; }
        public string aircraftregistration { get; set; }
        public string status { get; set; }
        public string publicRemark { get; set; }
        public string flightUniqueID { get; set; }
        public string amslinkedflightid { get; set; }

        public List<Dictionary<string, string>> route { get; set; } = new List<Dictionary<string, string>>();

        public List<Dictionary<string, string>> codeShares { get; set; } = new List<Dictionary<string, string>>();
        public FlightID linkedflightId { get; set; }
        //   public List<CustomField> Values { get; set; } = new List<CustomField>();

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> Events { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public List<Dictionary<string, string>> CheckInSlots { get; set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> StandSlots { get; set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> CarouselSlots { get; set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> GateSlots { get; set; } = new List<Dictionary<string, string>>();
        public Dictionary<string, List<Dictionary<string, string>>> CustomTables { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
        [JsonIgnore] 
        public string XmlRaw { get; set; }
        public string LastUpdated { get; private set; }

        public string Action { get;set; }
        public object Clone()
        {
            return (AMSFlight)MemberwiseClone();
        }
    }
    public class FlightID : ICloneable
    {
        [JsonIgnore]
        public string flightID { get; set; }
        public string flightNumber { get; set; }
        public string iataAirline { get; set; }
        public string icaoAirline { get; set; }
        public string iatalocalairport { get; set; }
        public string icaolocalairport { get; set; }
        public string scheduleDate { get; set; }
        public string scheduleTime { get; set; }

        [JsonIgnore]
        public DateTime scheduleDateTime
        {
            get
            {
                return DateTime.Parse(scheduleTime);
            }
        }
        public bool deleted { get; set; } = false;
        public string flightkind { get; set; }
        public string flightUniqueID { get; set; }

        public object Clone()
        {
            return (FlightID)MemberwiseClone();

        }
    }
    public class CustomField : ICloneable
    {
        public CustomField(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public string name { get; set; }
        public string value { get; set; }

        public object Clone()
        {
            return (CustomField)MemberwiseClone();
        }
    }
    public class CodeShare : ICloneable
    {
        public string airlineIATA { get; set; }
        public string airlineICAOet { get; set; }
        public string flightNUmber { get; set; }

        public object Clone()
        {
            return (CodeShare)MemberwiseClone();
        }

    }
    public class Subscription
    {
        public string? SubscriberToken { get; set; }
        public string? SubscriberName { get; set; }
        public string? SubscriptionID { get; set; }
        public string DataFormat { get; set; }  //JSON or XML
        public bool ChangeResourceGate { get; set; } = false;
        public bool ChangeResourceStand { get; set; } = false;
        public bool ChangeResourceCheckIn { get; set; } = false;
        public bool ChangeResourceBaggageReclaim { get; set; } = false;
        public bool ChangeEstimated { get; set; } = false;    
        public bool IsArrival { get; set; } = false;
        public bool IsDeparture { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public string? AirlineIATA { get; set; }
        public string? AirportIATA { get; set; }
        public int MaxHorizonInHours { get; set; } = 24;
        public int MinHorizonInHours { get; set; } = -24;
        public string? AuthorizationHeaderName { get; set; }
        public string? AuthorizationHeaderValue { get; set; }
        public string? CallBackURL { get; set; }
        public int ConsecutiveUnsuccessfullCalls { get; set; } = 0;
        public int ConsecutiveSuccessfullCalls { get; set; } = 0;
        public DateTime? LastSuccess { get; set; } = DateTime.MinValue;
        public DateTime? LastFailure { get; set; } = DateTime.MinValue;
        public string? LastError { get; set; }
        public DateTime ValidUntil { get; set; } = DateTime.MaxValue;

        [JsonIgnore]
        public int BacklogSize
        {
            get {
                return BackLog.Count;
                }
        }

        [JsonIgnore]
        public SubscriptionBacklog BackLog { get; set; } = new();

        public string? StatusMessage { get; set; }

        internal void SetConfig(GetFlightsConfig? config)
        {
            BackLog.SetConfig(config, SubscriptionID, SubscriberToken);
        }
    }
}
