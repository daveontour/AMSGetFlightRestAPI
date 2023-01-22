using AMSGetFlights.Model;
using Newtonsoft.Json;
using System.Net;
using System.Text;


namespace AMSGetFlights.Services
{
    public class SubscriptionDispatcher : IDisposable
    {
        FooQueue<AMSFlight> queue = new();
        EventExchange eventExchange;
        private SubscriptionManager? subManager;
        private GetFlightsConfigService configService;
        private FlightSanitizer sanitizer;


        public SubscriptionDispatcher(EventExchange eventExchange, GetFlightsConfigService configService, SubscriptionManager subManager, FlightSanitizer sanitizer)
        {
            this.eventExchange = eventExchange;
            this.configService = configService;
            this.subManager = subManager;
            this.sanitizer = sanitizer;

            ThreadPool.SetMinThreads(configService.config.MinNumSubscriptionThreads, 0);
            ThreadPool.SetMaxThreads(configService.config.MaxNumSubscriptionThreads, 0);
        }

        private void SendBacklogRequest(Subscription sub)
        {
            SendBacklog(sub);
        }


        public async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            await Task.Run(() => Start());
        }

        public async Task Start()
        {
            eventExchange.OnSendBacklog += SendBacklogRequest;
            eventExchange.OnFlightUpdated += FlightUpdated;
            eventExchange.OnFlightDeleted += FlightDeleted;
            eventExchange.OnFlightInserted += FlightAdded;
            queue.OnChanged += UpdatedEnqueue;
        }

        private void FlightAdded(AMSFlight obj)
        {
            //The subclass Enqueue also fires an event to initiate the transfer
            obj.Action = "insert";
            queue.Enqueue(obj);

        }
        private void FlightUpdated(AMSFlight obj)
        {
            //The subclass Enqueue also fires an event to initiate the transfer
            obj.Action = "update";
            queue.Enqueue(obj);

        }
        private void FlightDeleted(AMSFlight obj)
        {
            //The subclass Enqueue also fires an event to initiate the transfer
            obj.Action = "delete";
            queue.Enqueue(obj);

        }

        private void TaskCallBack(object state)
        {
            Tuple<Subscription, AMSFlight> info = state as Tuple<Subscription, AMSFlight>;

            Subscription sub = info.Item1;
            AMSFlight flight = info.Item2;


            Console.WriteLine($"Processing subscription {sub.SubscriptionID}. Flight {flight?.callsign}");
            eventExchange.TopStatusMessage($"Processing subscriptions for  {flight?.callsign}");

            if (!configService.config.Users.ContainsKey(sub.SubscriberToken))
            {
                Console.WriteLine("User not found for subscription token");
                eventExchange.TopStatusMessage("User not found for subscription token");
                return;
            }

            if (!sub.IsEnabled || sub.ValidUntil < DateTime.Now)
            {
                Console.WriteLine($"Subscription: {sub.SubscriptionID}. Disabled");
                return;
            }



            lock (sub.BackLog)
            {
                //Enqueue the new flight, and then process the Backlog queue until empty

                bool initByPassBacklog = flight == null;

                if (sub.BackLog.Count == 0)
                {
                    initByPassBacklog = true;
                }
                else
                {
                    // Put the flight omto the backlog so any exisiting messages are processed first 
                    sub.BackLog.Put(flight);
                }


                while (sub.BackLog.Count > 0 || initByPassBacklog)
                {
                    // If the Backlog is empty, then we canjust use the flight
                    AMSFlight fl = flight;

                    if (initByPassBacklog)
                    {
                        //Unset the flag so it doesn't keep repeating
                        initByPassBacklog = false;
                    }
                    else
                    {
                        // We are using the backlog, so get the next flight
                        fl = sub.BackLog.Next(configService.config);
                    }

                    if (fl is null) continue;

                    // Check whether the Flight has changes that the user is interested in
                    if (!fl.HasUserInterestedChanges(sub))
                    {
                        if (configService.config.IsTest) Console.WriteLine($"Sub:{sub.SubscriptionID} Failed HAS Interest");
                        continue;
                    }
                    if (!sub.IsArrival && fl.flightId.flightkind.ToLower() == "arrival")
                    {
                        if (configService.config.IsTest) Console.WriteLine($"Subscription: {sub.SubscriptionID} Failed Arrival");
                        continue;
                    }
                    if (!sub.IsDeparture && fl.flightId.flightkind.ToLower() == "departure")
                    {
                        if (configService.config.IsTest) Console.WriteLine($"Subscription: {sub.SubscriptionID} Failed Departure");
                        continue;
                    }
                    if (sub.AirportIATA != null)
                    {
                        if (sub.AirportIATA != fl.flightId.iatalocalairport)
                        {
                            if (configService.config.IsTest) Console.WriteLine($"Subscription: {sub.SubscriptionID} Failed Airport");
                            continue;
                        }
                    }
                    if (sub.AirlineIATA != null)
                    {
                        if (sub.AirlineIATA != fl.flightId.iataAirline)
                        {
                            if (configService.config.IsTest) Console.WriteLine($"Subscription: {sub.SubscriptionID} Failed Airline");
                            continue;
                        }
                    }

                    TimeSpan? ts = fl.flightId.scheduleDateTime - DateTime.Now;
                    if (ts.Value.TotalHours > sub.MaxHorizonInHours)
                    {
                        continue;
                    }
                    ts = DateTime.Now - fl.flightId.scheduleDateTime;
                    if (ts.Value.TotalHours < sub.MinHorizonInHours)
                    {
                        if (configService.config.IsTest) Console.WriteLine($"Subscription: {sub.SubscriptionID} Failed Timespan");
                        continue;
                    }

                    fl = sanitizer.SanitizeFlight(fl, sub.DataFormat=="XML", sub.SubscriberToken);

                    //// Customise for user
                    //// Adjust the result so only elements the user is allowed to see are set
                    //List<string> validFields = configService.config.ValidUserFields(sub.SubscriberToken);
                    //List<string> validCustomFields = configService.config.ValidUserCustomFields(sub.SubscriberToken);
                    //foreach (var prop in fl.GetType().GetProperties())
                    //{
                    //    if (prop.Name != "flightId" && prop.Name != "Key" && !validFields.Contains(prop.Name))
                    //    {
                    //        try
                    //        {
                    //            if (prop.Name == "XmlRaw" || prop.Name == "Action")
                    //            {
                    //                continue;
                    //            }
                    //            prop.SetValue(fl, null);
                    //        } catch(Exception ex)
                    //        {
                    //            Console.WriteLine(ex.Message);   
                    //        }
                    //    }
                    //}
                    //if (fl.Values != null && validCustomFields.Count() > 0)
                    //{
                    //    Dictionary<string, string> fields = new Dictionary<string, string>();
                    //    foreach (string key in fl.Values.Keys)
                    //    {
                    //        if (validCustomFields.Contains(key))
                    //        {
                    //            fields.Add(key, fl.Values[key]);
                    //        }
                    //    }
                    //    fl.Values = fields;
                    //    // flight.Values = flight.Values.Where(f => validCustomFields.Contains(f.name)).ToList();
                    //}

                    // Made it this far, so OK to try send the message
                    eventExchange.TopStatusMessage($"Sending Subscription Update for {fl.callsign}. Action = {fl.Action}");

                    string content = fl.XmlRaw;
                    string mediaType = "text/xml";

                    if (sub.DataFormat.ToLower() == "json")
                    {
                        mediaType = "text/json";
                        content = JsonConvert.SerializeObject(fl, Formatting.Indented, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    }

                    if (!configService.config.IsTest)
                    {
                        try
                        {
                            using var client = new HttpClient();
                            client.Timeout = TimeSpan.FromSeconds(30);

                            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, sub.CallBackURL)
                            {
                                Content = new StringContent(content, Encoding.UTF8, mediaType)
                            };

                            if (sub.AuthorizationHeaderName != null)
                            {

                                requestMessage.Headers.Add(sub.AuthorizationHeaderName, sub.AuthorizationHeaderValue);

                            }

                            using (requestMessage)
                            {
                                try
                                {
                                    using HttpResponseMessage response = client.SendAsync(requestMessage).Result;
                                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                                    {
                                        sub.ConsecutiveSuccessfullCalls++;
                                        sub.LastSuccess = DateTime.Now;
                                        sub.LastError = null;
                                        sub.ConsecutiveUnsuccessfullCalls = 0;
                                    }
                                    else
                                    {
                                        sub.ConsecutiveSuccessfullCalls = 0;
                                        sub.ConsecutiveUnsuccessfullCalls++;
                                        sub.LastFailure = DateTime.Now;
                                        sub.LastError = $"HTTP Status Code: {response.StatusCode}";
                                        sub.BackLog.Put(fl);
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sub.ConsecutiveSuccessfullCalls = 0;
                                    sub.ConsecutiveUnsuccessfullCalls++;
                                    sub.LastFailure = DateTime.Now;
                                    sub.LastError = ex.Message;
                                    sub.BackLog.Put(fl);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sub.ConsecutiveSuccessfullCalls = 0;
                            sub.ConsecutiveUnsuccessfullCalls++;
                            sub.LastFailure = DateTime.Now;
                            sub.LastError = ex.Message;
                            sub.BackLog.Put(fl);
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Writing {sub.SubscriptionID}. Flight {fl.callsign}. Action = {fl.Action}");
                    }
                }
            }
            eventExchange.TopStatusMessage($"Processing subscriptions for  {flight?.callsign} completed");
            eventExchange.SubscriptionSend();
        }

        public void Dispose()
        {
            eventExchange.OnFlightUpdated -= FlightUpdated;
            eventExchange.OnFlightInserted -= FlightAdded;
            eventExchange.OnFlightDeleted -= FlightDeleted;
            eventExchange.OnSendBacklog -= SendBacklogRequest;
            queue.OnChanged -= UpdatedEnqueue;
        }

        public string SendBacklog(Subscription s)
        {

            int depth = s.BackLog.Count;
            Tuple<Subscription, AMSFlight> state = new Tuple<Subscription, AMSFlight>(s, null);
            ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCallBack), state);

            return depth.ToString();

        }
        private void UpdatedEnqueue()
        {

            AMSFlight obj = queue.Dequeue();

            foreach (Subscription sub in subManager.Subscriptions)
            {
                Tuple<Subscription, AMSFlight> state = new Tuple<Subscription, AMSFlight>(sub, obj);
                ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCallBack), state);
            }
        }
    }



    public class FooQueue<T>
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<T> queue = new System.Collections.Concurrent.ConcurrentQueue<T>();
        public event Action OnChanged;

        public virtual void Enqueue(T item)
        {
            queue.Enqueue(item);
            OnChanged?.Invoke();
        }
        public int Count { get { return queue.Count; } }

        public virtual T Dequeue()
        {
            queue.TryDequeue(out T item);
            return item;
        }
    }
}

