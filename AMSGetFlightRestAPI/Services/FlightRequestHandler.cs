using AMSGetFlights.Model;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AMSGetFlights.Services
{
    public class FlightRequestHandler
    {
        private readonly FlightRepository repo;
        private readonly GetFlightsConfigService configService;
        private readonly EventExchange eventExchange;
        private FlightSanitizer sanitizer;

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

            //// Adjust the result so only elements the user is allowed to see are set
            //List<string> validFields = configService.config.ValidUserFields(query.token);
            //List<string> validCustomFields = configService.config.ValidUserCustomFields(query.token);
            
            //List<string> validCustomFieldKeys = new();
            //foreach(string f in validCustomFields)
            //{
            //    var key = configService.config.CustomFieldToParameter.FirstOrDefault(x => x.Value == f).Key;
            //    validCustomFieldKeys.Add(key);  
            //}

            
            //// Remove any data the user is not configured to see. 
            //foreach (var flight in res)
            //{

            //    if (IsXML)
            //        // Sanitize the XML to only contain allowed Custom Fields
            //    {
            //        try
            //        {
            //            XmlDocument doc = new XmlDocument();
            //            doc.LoadXml(flight.XmlRaw);

            //            XmlNode flightStateNode = doc.SelectSingleNode(".//FlightState");
            //            foreach (XmlNode node in flightStateNode.SelectNodes("./Value"))
            //            {
            //                // Remove all the Custome fields if not configured
            //                if (!validFields.Contains("Values"))
            //                {
            //                    flightStateNode.RemoveChild(node);
            //                    continue;
            //                }

            //                // Subset of Custom fields are allowed.
            //                string prop = node.Attributes["propertyName"].Value;
            //                if (validCustomFieldKeys.Contains(prop))
            //                {
            //                    continue;
            //                }
            //                flightStateNode.RemoveChild(node);
            //            }

            //            foreach (XmlNode node in flightStateNode.SelectNodes("./TableValue"))
            //            {
            //                // Remove all the Custome Tables if not configured
            //                if (!validFields.Contains("CustomTables"))
            //                {
            //                    flightStateNode.RemoveChild(node);
            //                    continue;
            //                }

            //                // Subset of Custom fields are allowed.
            //                string prop = node.Attributes["propertyName"].Value;
            //                if (validCustomFieldKeys.Contains(prop))
            //                {
            //                    continue;
            //                }
            //                flightStateNode.RemoveChild(node);
            //            }

            //            if (!validFields.Contains("StandSlots"))
            //            {
            //                XmlNode node = flightStateNode.SelectSingleNode(".//StandSlots");
            //                if (node != null)
            //                {
            //                    try
            //                    {
            //                        flightStateNode.RemoveChild (node);
            //                    } catch (Exception ex)
            //                    {
            //                        Console.WriteLine(ex.Message);  
            //                    }
            //                }
            //            }
            //            if (!validFields.Contains("GateSlots"))
            //            {
            //                XmlNode node = flightStateNode.SelectSingleNode(".//GateSlots");
            //                if (node != null)
            //                {
            //                    try
            //                    {
            //                        flightStateNode.RemoveChild(node);
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Console.WriteLine(ex.Message);
            //                    }
            //                }
            //            }
            //            if (!validFields.Contains("CarouselSlots"))
            //            {
            //                XmlNode node = flightStateNode.SelectSingleNode(".//CarouselSlots");
            //                if (node != null)
            //                {
            //                    try
            //                    {
            //                        flightStateNode.RemoveChild(node);
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Console.WriteLine(ex.Message);
            //                    }
            //                }
            //            }
            //            if (!validFields.Contains("CheckInSlots"))
            //            {
            //                XmlNode node = flightStateNode.SelectSingleNode(".//CheckInSlots");
            //                if (node != null)
            //                {
            //                    try
            //                    {
            //                        flightStateNode.RemoveChild(node);
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Console.WriteLine(ex.Message);
            //                    }
            //                }
            //            }

            //            flight.XmlRaw = PrintXML(doc);

            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine(ex.ToString());
            //        }


            //    }

            //    if (!IsXML)
            //    {
            //        foreach (var prop in flight.GetType().GetProperties())
            //        {
            //            if (prop.Name != "flightId" && prop.Name != "Key" && !validFields.Contains(prop.Name))
            //            {
            //                if (prop.Name == "XmlRaw" && IsXML)
            //                {
            //                    continue;
            //                }
            //                prop.SetValue(flight, null);
            //            }
            //        }
            //    }
            //    if (flight.Values != null && validCustomFields.Count() > 0 && !IsXML)
            //    {
            //        Dictionary<string, string> fields = new Dictionary<string, string>();
            //        foreach (string key in flight.Values.Keys)
            //        {
            //            if (validCustomFields.Contains(key))
            //            {
            //                fields.Add(key, flight.Values[key]);
            //            }
            //        }
            //        flight.Values = fields;
            //        // flight.Values = flight.Values.Where(f => validCustomFields.Contains(f.name)).ToList();
            //    }
            //    if (validCustomFields == null || validCustomFields.Count() == 0)
            //    {
            //        flight.Values = null;
            //    }
            //}

            eventExchange.APIRequestMade(query);

            return result;
        }

        public string PrintXML(XmlDocument document)
        {
            string result;

            MemoryStream mStream = new();
            XmlTextWriter writer = new(mStream, Encoding.Unicode);
            

            try
            {


                writer.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (Exception)
            {
                return "<Error><Error>";
            }

            mStream.Close();
            writer.Close();
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

            // Adjust the result so only elements the user is allowed to see are set
            List<string> validFields = configService.config.ValidUserFields(query.token);
            List<string> validCustomFields = configService.config.ValidUserCustomFields(query.token);

            foreach (var flight in flights)
            {
                foreach (var prop in flight.GetType().GetProperties())
                {
                    if (prop.Name != "flightId" && prop.Name != "Key" && !validFields.Contains(prop.Name))
                    {
                        if (prop.Name == "XmlRaw" && IsXML)
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

        public List<AMSFlight> GetSingleFlight(string xml, string token, bool IsXML = false)
        {
            AMSFlight flight = new(xml, configService.config);


            // Adsjust the result so only elements the user is allowed to see are set
            List<string> validFields = configService.config.ValidUserFields(token);
            List<string> validCustomFields = configService.config.ValidUserCustomFields(token);

            foreach (var prop in flight.GetType().GetProperties())
            {
                if (prop.Name != "flightId" && prop.Name != "Key" && !validFields.Contains(prop.Name))
                {
                    if (prop.Name == "XmlRaw" && IsXML)
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
