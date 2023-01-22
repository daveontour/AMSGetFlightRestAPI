using AMSGetFlights.Model;
using Radzen;
using System.Text;
using System.Xml;

namespace AMSGetFlights.Services
{
    public class FlightSanitizer
    {
        private GetFlightsConfigService configService;

        public FlightSanitizer(GetFlightsConfigService configService)
        {
            this.configService = configService;
        }
        public List<AMSFlight> SanitizeFlights(List<AMSFlight> flights,bool IsXML, string userToken)
        {
            List<string> validFields = configService.config.ValidUserFields(userToken);
            List<string> validCustomFields = configService.config.ValidUserCustomFields(userToken);
            List<string> validCustomFieldKeys = new();
            foreach (string f in validCustomFields)
            {
                var key = configService.config.CustomFieldToParameter.FirstOrDefault(x => x.Value == f).Key;
                validCustomFieldKeys.Add(key);
            }

            foreach(AMSFlight f in flights)
            {
                SanitizeFlight(f, IsXML, userToken, validFields, validCustomFields, validCustomFieldKeys);
            }

            return flights;
        }

        public AMSFlight SanitizeFlight(AMSFlight flight, bool IsXML, string userToken, List<string> validFields = null, List<string> validCustomFields = null, List<string> validCustomFieldKeys = null)
        {
            if(validFields == null)
            {
                validFields = configService.config.ValidUserFields(userToken);
                validCustomFields = configService.config.ValidUserCustomFields(userToken);
                validCustomFieldKeys = new();
                foreach (string f in validCustomFields)
                {
                    var key = configService.config.CustomFieldToParameter.FirstOrDefault(x => x.Value == f).Key;
                    validCustomFieldKeys.Add(key);
                }
            }

            if (IsXML)
            // Sanitize the XML to only contain allowed Custom Fields
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(flight.XmlRaw);

                    XmlNode flightStateNode = doc.SelectSingleNode(".//FlightState");
                    foreach (XmlNode node in flightStateNode.SelectNodes("./Value"))
                    {
                        // Remove all the Custome fields if not configured
                        if (!validFields.Contains("Values"))
                        {
                            flightStateNode.RemoveChild(node);
                            continue;
                        }

                        // Subset of Custom fields are allowed.
                        string prop = node.Attributes["propertyName"].Value;
                        if (validCustomFieldKeys.Contains(prop))
                        {
                            continue;
                        }
                        flightStateNode.RemoveChild(node);
                    }

                    foreach (XmlNode node in flightStateNode.SelectNodes("./TableValue"))
                    {
                        // Remove all the Custome Tables if not configured
                        if (!validFields.Contains("CustomTables"))
                        {
                            flightStateNode.RemoveChild(node);
                            continue;
                        }

                        // Subset of Custom fields are allowed.
                        string prop = node.Attributes["propertyName"].Value;
                        if (validCustomFieldKeys.Contains(prop))
                        {
                            continue;
                        }
                        flightStateNode.RemoveChild(node);
                    }

                    if (!validFields.Contains("StandSlots"))
                    {
                        XmlNode node = flightStateNode.SelectSingleNode(".//StandSlots");
                        if (node != null)
                        {
                            try
                            {
                                flightStateNode.RemoveChild(node);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    if (!validFields.Contains("GateSlots"))
                    {
                        XmlNode node = flightStateNode.SelectSingleNode(".//GateSlots");
                        if (node != null)
                        {
                            try
                            {
                                flightStateNode.RemoveChild(node);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    if (!validFields.Contains("CarouselSlots"))
                    {
                        XmlNode node = flightStateNode.SelectSingleNode(".//CarouselSlots");
                        if (node != null)
                        {
                            try
                            {
                                flightStateNode.RemoveChild(node);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    if (!validFields.Contains("CheckInSlots"))
                    {
                        XmlNode node = flightStateNode.SelectSingleNode(".//CheckInSlots");
                        if (node != null)
                        {
                            try
                            {
                                flightStateNode.RemoveChild(node);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }

                    flight.XmlRaw = PrintXML(doc);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }


            }

            if (!IsXML)
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
            }
            if (flight.Values != null && validCustomFields.Count() > 0 && !IsXML)
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
            if (validCustomFields == null || validCustomFields.Count() == 0)
            {
                flight.Values = null;
            }
            return flight;
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
    }
}
