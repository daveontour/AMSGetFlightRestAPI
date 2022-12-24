﻿using AMSGetFlights.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Xml;
using NLog;
using AMSGetFlights.Services;
using Microsoft.AspNetCore.Connections;
using Radzen;
using Microsoft.AspNetCore.Http.Extensions;

namespace AMSGetFlights.Controllers
{
    [Route("api")]
    [ApiController]
    public partial class FlightRequestAPIController : ControllerBase
    {
        private IFlightRequestHandler handler;
        private IGetFlightsConfigService configService;
        private readonly Logger logger = LogManager.GetLogger("consoleLogger");

        public FlightRequestAPIController(IFlightRequestHandler handler, IGetFlightsConfigService configService)
        {
            this.handler = handler;
            this.configService = configService;
        }

        [HttpGet("status")]
        public ActionResult<ServerStatus> GetStatus()
        {
            try
            {
                ServerStatus status = new ServerStatus();
                GetFlightQueryObject query = handler.GetQueryObject(Request, null);


                if (query.token == "default" && !configService.config.AllowAnnonymousUsers)
                {
                    status.Error = "Not Authorized";
                    return status;
                }

                Process currentProcess = Process.GetCurrentProcess();
                long usedMemory = currentProcess.PrivateMemorySize64;

                status.EarliestEntry = handler.repo.MinDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                status.LatestEntry = handler.repo.MaxDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                status.NumberOfEntries = handler.repo.GetNumEntries();
                status.ProcessMemory = usedMemory;

                return status;
            } catch (Exception e)
            {
                ServerStatus status = new ServerStatus();
                status.Error = "Error";
                return status;
            }
            finally
            {
                System.GC.Collect();
            }
        }

        [HttpGet("GetFlightsJSON")]
        public ActionResult<GetFlightsResponse> Get()
        {

            handler.URLRequestMade(Request.GetDisplayUrl());

            // Are JSON requests allowed
            if (!configService.config.AllowJSONFormat)
            {
               
                Log("JSON Request. Not enabled", warn: true);
                return new GetFlightsResponse() { error = "Not Enabled" };
            }

            try
            {
                // Parse the input
                GetFlightQueryObject query = handler.GetQueryObject(Request, "JSON");

                //Check for Annonymous users
                if (query.token == "default" && !configService.config.AllowAnnonymousUsers)
                {
                    Log("Not Authorized", query, warn: true);
                    return new GetFlightsResponse() { error = "Not Authorized" };
                }

                // Check the provided user token exists
                if (!configService.config.Users.ContainsKey(query.token))
                {
                    Log("User Not Found", query, warn: true);
                    return new GetFlightsResponse() { error = "User Not Found" };
                }

                // Check the provided user token is enabled
                if (!configService.config.Users[query.token].Enabled)
                {
                    Log("User Disabled", query, warn: true);
                    return new GetFlightsResponse() { error = "User Disabled" };
                }

                // Check the query agains the current contents of the cache
                string queryStatus = handler.CheckQueryStatus(query);

                // If configured go directly to AMS for a specific flight if out of bounds
                if ((queryStatus =="OUTOFBOUND" || queryStatus == "PARTIAL") && configService.config.EnableDirectAMSLookukOnSingleFlightCacheFailure  && query.IsSingleFlight)
                {
                    string xml = GetOutOfBoundsFlight(query);
                    if (xml == "Flight Not Found")
                    {
                        return new GetFlightsResponse() { error = "Flight Not Found", query = query, flights = new List<AMSFlight>() };
                    }

                    List<AMSFlight> tt = handler.GetSingleFlight(xml, query.token);
                    query.NumberOfResults = tt.Count();

                    Log("Success", query, query.NumberOfResults.ToString(), warn: true); 
 
                    GetFlightsResponse res = new GetFlightsResponse() { query = query, flights = tt, partialResutlsRetuned = queryStatus == "PARTIAL" ? true : false };
                    Response.StatusCode = StatusCodes.Status200OK;

                    return res;
                } else if ((queryStatus == "OUTOFBOUND" || queryStatus == "PARTIAL") && configService.config.EnableDirectAMSLookukOnMultiFlightCacheFailure)
                {
                    string xml = GetOutOfBoundsFlights(query);
                    if (xml == "Flights Not Found")
                    {
                        return new GetFlightsResponse() { error = "Flights Not Found", query = query, flights = new List<AMSFlight>() };
                    }

                    List<AMSFlight> tt = handler.GetFlightsFromXML(xml,query);
                    query.NumberOfResults = tt.Count();

                    Log("Success", query, query.NumberOfResults.ToString(), warn: true);

                    GetFlightsResponse res = new GetFlightsResponse() { query = query, flights = tt, partialResutlsRetuned = queryStatus == "PARTIAL" ? true : false };
                    Response.StatusCode = StatusCodes.Status200OK;

                    return res;
                }
                
                // Query is completely out of bounds of the current cache
                if (queryStatus == "OUTOFBOUND")
                {
                    Log("Out of Bounds", query, warn: true);
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    return new GetFlightsResponse() { error = "Requested Dates are Out of Bounds" };


                // Query is partiall out of bounds, so check if partial results are OK
                } else  if (queryStatus == "PARTIAL" && query.partialResults.ToLower() == "false")
                {
                    Log("Partially Out of Bounds", query, warn: true);
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    return new GetFlightsResponse() { error = "Requested Dates are Parrtially Out of Bounds" };
                }

                //Get the flights
                List<AMSFlight> t = handler.GetFlights(query);
                query.NumberOfResults = t.Count();

                // Return the results
                Log("Success", query, query.NumberOfResults.ToString(),info: true);
                GetFlightsResponse r = new GetFlightsResponse() { query = query, flights = t, partialResutlsRetuned = queryStatus == "PARTIAL" ? true : false };
                Response.StatusCode = StatusCodes.Status200OK;

                return r;
            }
            catch (Exception ex)
            {
                Log($"Query String: {Request.QueryString.ToString()}. Error Message {ex.Message}", error: true);

                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return new GetFlightsResponse() { error = $"{ex.Message}" };
            }
            finally
            {
                System.GC.Collect();
            }
        }

        [HttpGet("GetFlightsXML")]
        public IActionResult GetXML()
        {
            handler.URLRequestMade(Request.GetDisplayUrl());

            if (!configService.config.AllowAMSXFormat)
            {
                Log("XML Request. Not enabled", warn: true);
                return new ContentResult
                {
                    Content = $"<error>XML Request Not Enabled</error>",
                    ContentType = "text/xml",
                    StatusCode = 400
                };
            }
            try
            {
                GetFlightQueryObject query = handler.GetQueryObject(Request, "XML");
                if (query.token == "default" && !configService.config.AllowAnnonymousUsers)
                {
                    Log("Not Authorised",query, warn: true);
                    return new ContentResult
                    {
                        Content = $"<error>Not Authorised</error>",
                        ContentType = "text/xml",
                        StatusCode = 400
                    };
                }

                // Check the provided user token exists
                if (!configService.config.Users.ContainsKey(query.token))
                {
                    Log("User Not Found", query, warn: true);
                    return new ContentResult
                    {
                        Content = $"<error>User Not Found</error>",
                        ContentType = "text/xml",
                        StatusCode = 400
                    };
                }
                if (!configService.config.Users[query.token].Enabled)
                {
                     Log("User Disabled", query, warn: true);

                    return new ContentResult
                    {
                        Content = $"<error>User Disabled</error>",
                        ContentType = "text/xml",
                        StatusCode = 400
                    };
                }

                string queryStatus = handler.CheckQueryStatus(query);

                if ((queryStatus == "OUTOFBOUND" || queryStatus == "PARTIAL") && configService.config.EnableDirectAMSLookukOnSingleFlightCacheFailure && query.IsSingleFlight)
                {
                    string xml = GetOutOfBoundsFlight(query);
                    if (xml == "Flight Not Found")
                    {
                        return new ContentResult
                        {
                            Content = $"<error>Flight Not Found</error>",
                            ContentType = "text/xml",
                            StatusCode = 500
                        };
                    }

                    List<AMSFlight> tt = handler.GetSingleFlight(xml, query.token);
                    query.NumberOfResults = tt.Count();

                    StringBuilder sbx = new StringBuilder();
                    sbx.AppendLine("<Flights>");
                    foreach (AMSFlight flight in tt)
                    {
                        sbx.AppendLine(flight.XmlRaw);
                    }
                    sbx.AppendLine("</Flights>");

                    Log("Success", query, info: true);
                    return new ContentResult
                    {
                        Content = PrintXML(sbx.ToString()),
                        ContentType = "text/xml",
                        StatusCode = (int)HttpStatusCode.OK
                    };
                }
                else if ((queryStatus == "OUTOFBOUND" || queryStatus == "PARTIAL") && configService.config.EnableDirectAMSLookukOnMultiFlightCacheFailure)
                {
                    string xml = GetOutOfBoundsFlights(query);
                    if (xml == "Flight Not Found")
                    {
                        return new ContentResult
                        {
                            Content = $"<error>Flight Not Found</error>",
                            ContentType = "text/xml",
                            StatusCode = 500
                        };
                    }

                    List<AMSFlight> tt = handler.GetFlightsFromXML(xml, query);
                    query.NumberOfResults = tt.Count();

                    StringBuilder sbx = new StringBuilder();
                    sbx.AppendLine("<Flights>");
                    foreach (AMSFlight flight in tt)
                    {
                        sbx.AppendLine(flight.XmlRaw);
                    }
                    sbx.AppendLine("</Flights>");

                    Log("Sucess", query, info: true);

                    return new ContentResult
                    {
                        Content = PrintXML(sbx.ToString()),
                        ContentType = "text/xml",
                        StatusCode = (int)HttpStatusCode.OK
                    };
                }

                if (queryStatus == "OUTOFBOUND")
                {
                    Log("Out of Bounds", query, warn: true);
                    return new ContentResult
                    {
                        Content = $"<error>Requested Dates are out of Bounds</error>",
                        ContentType = "text/xml",
                        StatusCode = 400
                    };
                }

                if (queryStatus == "PARTIAL" && query.partialResults.ToLower() == "false")
                {
                    Log("Partially Out of Bounds", query, warn: true);

                    return new ContentResult
                    {
                        Content = $"<error>Requested Dates are Partially out of Bounds</error>",
                        ContentType = "text/xml",
                        StatusCode = 400
                    };
                }

                List<AMSFlight> t = handler.GetFlights(query, true);
                query.NumberOfResults = t.Count();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<Flights>");
                foreach (AMSFlight flight in t)
                {
                    sb.AppendLine(flight.XmlRaw);
                }
                sb.AppendLine("</Flights>");

                Log("Sucess", query, info: true);

                 return new ContentResult
                {
                    Content = PrintXML(sb.ToString()),
                    ContentType = "text/xml",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    Content = $"<error>{ex.Message}</error>",
                    ContentType = "text/xml",
                    StatusCode = 500
                };
            } finally
            {
                System.GC.Collect();
            }
        }
        private string GetOutOfBoundsFlight(GetFlightQueryObject query)
        {
            string? token = null;
            string? url = null;

            foreach (AirportSource apts in configService.config.Airports)
            {
                if (query.apt != apts.AptCode) continue;
                token = apts.Token;
                url = apts.WSURL;
                break;
            }

            if (token == null || url == null)
            {
                return "Flight Not Found";
            }

            string? xml;
            if (query.type != null && query.type != "both")
            {
                xml = AMSGetFlightsStatusService.GetFlightXML(query, query.type, token, url).Result;
            }
            else
            {
                xml = AMSGetFlightsStatusService.GetFlightXML(query, "Arrival", token, url).Result;
                if (xml == null || xml.Contains("<ErrorCode>FLIGHT_NOT_FOUND</ErrorCode>"))
                {
                    xml = AMSGetFlightsStatusService.GetFlightXML(query, "Departure", token, url).Result;
                }
            }

            if (xml == null || xml.Contains("<ErrorCode>FLIGHT_NOT_FOUND</ErrorCode>"))
            {
                return "Flight Not Found";
            }
            return xml; 
        }

        private string GetOutOfBoundsFlights(GetFlightQueryObject query)
        {
            string? token = null;
            string? url = null;

            foreach (AirportSource apts in configService.config.Airports)
            {
                if (query.apt != apts.AptCode) continue;
                token = apts.Token;
                url = apts.WSURL;
                break;
            }

            if (token == null || url == null)
            {
                return "Flight Not Found";
            }

            string? xml = AMSGetFlightsStatusService.GetFlightsXML(query.startQuery, query.endQuery, query.apt, token, url).Result;
            if (xml == null || xml.Contains("<ErrorCode>FLIGHT_NOT_FOUND</ErrorCode>"))
            {
                return "Flight Not Found";
            }
            return xml;
        }

        private void Log(string result,  GetFlightQueryObject? query = null, string? recordsReturned = null, bool info = false, bool warn = false, bool error = false)
        {
            LogEntry lee = new LogEntry();
            if (result != null) {
                lee.Result = result;
            }
            if (query != null)
            {
                lee.query = query;
            }
            if(int.TryParse(recordsReturned, out int count))
            {
                lee.RecordsReturned = count;
            }
            handler.MonitorMessage(result);

            if (error)logger.Error(JsonConvert.SerializeObject(lee, Newtonsoft.Json.Formatting.Indented));
            if (warn) logger.Warn(JsonConvert.SerializeObject(lee, Newtonsoft.Json.Formatting.Indented));
            if (info) logger.Info(JsonConvert.SerializeObject(lee, Newtonsoft.Json.Formatting.Indented));
        }
        public string PrintXML(string xml)
        {
            string result = "";

            MemoryStream mStream = new();
            XmlTextWriter writer = new(mStream, Encoding.Unicode);
            XmlDocument document = new();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

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
