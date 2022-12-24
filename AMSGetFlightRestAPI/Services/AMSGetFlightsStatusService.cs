
using System.Net;
using System.Text;
using System.Xml;
using Experimental.System.Messaging;
using AMSGetFlights.Model;
using System.Diagnostics;
using Quartz.Impl;
using Quartz;
using NLog;

namespace AMSGetFlights.Services;

public class AMSGetFlightsStatusService : IAMSGetFlightStatusService
{
    public static Action? OnServerFlightsUpdates;
    public static Action? OnServerNoFlightsUpdates;
    public static Action<bool>? OnFlightServiceRunning;
    public static Action<string>? OnConsoleMessage;

    public bool Running { get; set; } = false;
    private bool startListenLoop;
    private IFlightRepository repo;
    private int advanceWindow = 10;
    private int backWindow = -3;
    private int chunkSize = 1;
    private List<string> listenerQueues = new List<string>();   
    public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
    private IGetFlightsConfigService configService;
    private int i = 1;
    private static AMSGetFlightsStatusService Instance { get; set; }

    public AMSGetFlightsStatusService(IFlightRepository repo, IGetFlightsConfigService configService)
    {
        this.repo = repo;
        this.configService = configService;

        advanceWindow = configService.config.ForewardWindowInDays;
        backWindow = configService.config.BackwardWindowInDays;
        chunkSize = configService.config.ChunkSizeInDays;

        Instance = this;
        //Scheduler for the refressh job
        StdSchedulerFactory factory = new StdSchedulerFactory();
        IScheduler scheduler = factory.GetScheduler().Result;

        // and start it off
        scheduler.Start().Wait();

        // Schedule the jobs to refresh the cache and update the status of tows
        IJobDetail job = JobBuilder.Create<RefreshJob>().WithIdentity("job1", "group1").Build();
        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .WithCronSchedule(configService.config.RefreshCron)
        .Build();


        scheduler.ScheduleJob(job, trigger);
    }
    public async Task Start()
    {
        advanceWindow = configService.config.ForewardWindowInDays;
        backWindow = configService.config.BackwardWindowInDays;
        chunkSize = configService.config.ChunkSizeInDays;


        // Purge the listener queues and start listening
        foreach (AirportSource airport in configService.config.GetAirports())
        {
            try
            {

                startListenLoop = true;
                Thread receiveThread = new Thread(() => ListenToQueue(airport.NotificationQueue))
                {
                    IsBackground = false
                };
                receiveThread.Start();


            }
            catch (Exception ex)
            {
                logger.Error("Error starting notification queue listener");
                logger.Error(ex.Message);
            }
        }

        // Initial population
        await Task.Run(() => PopulateFlightCache());

        //   Scheduler for the refressh job
        StdSchedulerFactory factory = new StdSchedulerFactory();
        IScheduler scheduler = factory.GetScheduler().Result;

        // and start it off
        scheduler.Start().Wait();

        // Schedule the jobs to refresh the cache and update the status of tows
        IJobDetail job = JobBuilder.Create<RefreshJob>().WithIdentity("job1", "group1").Build();
        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .WithCronSchedule(configService.config.RefreshCron)
        .Build();


        scheduler.ScheduleJob(job, trigger);

        Running = true;
        OnFlightServiceRunning?.Invoke(Running);
    }
    public async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        await Task.Run(() => Start());
    }
    public void PopulateFlightCache()
    {
        DateTime FromTime = DateTime.UtcNow.AddDays(backWindow);
        DateTime ToTime = DateTime.UtcNow.AddDays(advanceWindow);

        foreach (AirportSource airport in configService.config.GetAirports())
        {

            DateTime chunkFromTime = FromTime;
            DateTime chunkToTime = chunkFromTime.AddDays(chunkSize);

            do
            {
                OnConsoleMessage?.Invoke($"Fetching flight for airport {airport.AptCode} From: {chunkFromTime} To: {chunkToTime}");
                Console.WriteLine($"Fetching flight for airport {airport.AptCode} From: {chunkFromTime} To: {chunkToTime}");
                logger.Info($"Fetching flight for airport {airport.AptCode} From: {chunkFromTime} To: {chunkToTime}");

                string xml = GetFlightsXML(chunkFromTime, chunkToTime, airport.AptCode, airport.Token, airport.WSURL).Result;
                if (xml != null)
                {
                    ProcessMessageGet(xml);
                }
                repo.MinDateTime = FromTime;
                repo.MaxDateTime = chunkToTime;

                chunkFromTime = chunkToTime;
                chunkToTime = chunkFromTime.AddHours(24);

            } while (ToTime.AddDays(1) > chunkToTime.AddDays(1));
        }

        repo.PruneRepo(backWindow);

        repo.MinDateTime = FromTime;
        repo.MaxDateTime = ToTime;
    }
    public void UpdateFlightCache()
    {
        //Just need to fetch flight for the top end of the window

        Console.WriteLine("Running Update Job");
        OnConsoleMessage?.Invoke("Running Update Job");
        DateTime FromTime = DateTime.UtcNow.AddDays(advanceWindow - 2);
        DateTime ToTime = DateTime.UtcNow.AddDays(advanceWindow);

        foreach (AirportSource airport in configService.config.GetAirports())
        {

            DateTime chunkFromTime = FromTime;
            DateTime chunkToTime = chunkFromTime.AddDays(chunkSize);

            do
            {
                Console.WriteLine($"Update Job Fetching flight for airport {airport.AptCode} From: {chunkFromTime} To: {chunkToTime}");
                logger.Info($"Update Job Fetching flight for airport {airport.AptCode} From: {chunkFromTime} To: {chunkToTime}");
                string xml = GetFlightsXML(chunkFromTime, chunkToTime, airport.AptCode, airport.Token, airport.WSURL).Result;
                if (xml != null)
                {
                    ProcessMessageGet(xml);
                }

                chunkFromTime = chunkToTime;
                chunkToTime = chunkFromTime.AddHours(24);

            } while (ToTime.AddDays(1) > chunkToTime.AddDays(1));


        }
        repo.PruneRepo(backWindow);

        repo.MinDateTime = DateTime.UtcNow.AddDays(backWindow);
        repo.MaxDateTime = DateTime.UtcNow.AddDays(advanceWindow);
    }
    private void ListenToQueue(string? notificationQueue)
    {
        if (listenerQueues.Contains(notificationQueue))
        {
            return;
        }

        listenerQueues.Add(notificationQueue);  

        MessageQueue recvQueue = new MessageQueue(notificationQueue);
        recvQueue.Purge();

        // Listen to messages on the incoming queue
        while (startListenLoop)
        {
            //Put it in a Try/Catch so no bad message or reading problem dont stop the system
            try
            {
                using (Message msg = recvQueue.Receive(new TimeSpan(0, 0, 5)))
                {
                    string xml;
                    using (StreamReader reader = new StreamReader(msg.BodyStream))
                    {
                        xml = reader.ReadToEnd()
                            .Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-datatypes\"", "")
                            .Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-messages\"", "");
                    }
                    ProcessMessage(xml);
                }
            }
            catch (MessageQueueException)
            {
                // Handle other sources of a MessageQueueException.
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());    
                Thread.Sleep(5000);
            }
        }
    }
    private void ProcessMessage(string xml)
    {
        if (!xml.Contains("FlightUpdatedNotification") && !xml.Contains("FlightCreatedNotification") && !xml.Contains("FlightDeletedNotification"))
        {
            return;
        }
        if (xml.Contains("FlightDeletedNotification"))
        {
            XmlDocument xmlDocd = new XmlDocument();
            xmlDocd.LoadXml(xml);

            AMSFlight fld = new AMSFlight(xmlDocd, configService.config);
            repo.DeleteFlight(fld);
            return;
        }
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);

        AMSFlight fl = new AMSFlight(xmlDoc, configService.config);
        repo.UpdateOrAddFlight(fl);
        System.GC.Collect();
    }
    private void ProcessMessageGet(string xml)
    {
        if (!xml.Contains("<Flight>"))
        {
            return;
        }
        xml = xml.Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-datatypes\"", "")
                 .Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-messages\"", "")
                 .Replace("xmlns=\"http://www.sita.aero/ams6-xml-api-webservice\"", "");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);

        List<AMSFlight> fls = new List<AMSFlight>();
        foreach (XmlNode flnode in xmlDoc.SelectNodes("//*[local-name() = 'Flights']/*[local-name() = 'Flight']"))
        {
            AMSFlight fl = new AMSFlight(flnode, configService.config);
            fls.Add(fl);
        }
        repo.BulkUpdateOrInsert(fls);
        System.GC.Collect();
    }
    public async static Task<string> GetFlightsXML(DateTime from, DateTime to, string aptCode, string token, string url)
    {

        string mediaType = "text/xml";
        string messageXML = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"">
                               <soapenv:Header />
                               <soapenv:Body>
                                  <ams6:GetFlights>
                                     <ams6:sessionToken>{{token}}</ams6:sessionToken>
                                     <ams6:from>{{from}}</ams6:from>
                                     <ams6:to>{{to}}</ams6:to>
                                     <ams6:airport>{{airport}}</ams6:airport>
                                     <ams6:airportIdentifierType>IATACode</ams6:airportIdentifierType>
                                  </ams6:GetFlights>
                               </soapenv:Body>
                            </soapenv:Envelope>";

        messageXML = messageXML.Replace("{{token}}", token)
            .Replace("{{airport}}", aptCode)
            .Replace("{{from}}", from.ToString("yyyy-MM-ddTHH:mm:ss"))
            .Replace("{{to}}", to.ToString("yyyy-MM-ddTHH:mm:ss"));

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);


            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(messageXML, Encoding.UTF8, mediaType)
            };

            requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/GetFlights");

            using (requestMessage)
            {
                try
                {
                    using HttpResponseMessage response = await client.SendAsync(requestMessage);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }
    public async static Task<string> GetFlightXML(GetFlightQueryObject query, string kind, string token, string url)
    {
        return await GetFlightXML(query.al, query.flt, kind, query.schedDate, query.apt, token, url);
    }
    private async static Task<string> GetFlightXML(string airline, string flt, string kind, string sdo, string aptCode, string token, string url)
    {
        string mediaType = "text/xml";
        string messageXML = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"" xmlns:wor=""http://schemas.datacontract.org/2004/07/WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes"">
                               <soapenv:Header/>
                               <soapenv:Body>
                                  <ams6:GetFlight>
                                     <ams6:sessionToken>{{token}}</ams6:sessionToken>
                                     <ams6:flightId>
                                        <wor:_hasAirportCodes>true</wor:_hasAirportCodes>
                                        <wor:_hasFlightDesignator>true</wor:_hasFlightDesignator>
                                         <wor:_hasScheduledTime>false</wor:_hasScheduledTime>
                                        <wor:airlineDesignatorField>
                                           <wor:LookupCode>
                                              <wor:codeContextField>IATA</wor:codeContextField>
                                              <wor:valueField>{{airline}}</wor:valueField>
                                           </wor:LookupCode>
                                        </wor:airlineDesignatorField>
                                        <wor:airportCodeField>
                                           <wor:LookupCode>
                                              <wor:codeContextField>IATA</wor:codeContextField>
                                              <wor:valueField>{{apt}}</wor:valueField>
                                           </wor:LookupCode>
                                        </wor:airportCodeField>
                                        <wor:flightKindField>{{kind}}</wor:flightKindField>
                                        <wor:flightNumberField>{{flt}}</wor:flightNumberField>
                                        <wor:scheduledDateField>{{date}}</wor:scheduledDateField>
                                     </ams6:flightId>
                                  </ams6:GetFlight>
                               </soapenv:Body>
                            </soapenv:Envelope>";

        messageXML = messageXML.Replace("{{token}}", token)
            .Replace("{{apt}}", aptCode)
            .Replace("{{flt}}", flt)
            .Replace("{{airline}}", airline)
            .Replace("{{kind}}", kind)
            .Replace("{{date}}", sdo);

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);


            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(messageXML, Encoding.UTF8, mediaType)
            };

            requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/GetFlight");

            using (requestMessage)
            {
                try
                {
                    using HttpResponseMessage response = await client.SendAsync(requestMessage);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }
    internal class RefreshJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            AMSGetFlightsStatusService ts = Instance;
            return Task.Run(() => { ts.UpdateFlightCache(); });
        }
    }
}