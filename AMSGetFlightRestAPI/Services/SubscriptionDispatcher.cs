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
        private IGetFlightsConfigService configService;


        public SubscriptionDispatcher(EventExchange eventExchange, IGetFlightsConfigService configService)
        {
            this.eventExchange = eventExchange;
            this.configService = configService;            
        }

        public void SetSubscriptionManager(SubscriptionManager subManager)
        {
            this.subManager = subManager;
        }

        public async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            await Task.Run(() => Start());
        }

        public async Task Start()
        {
            eventExchange.OnFlightUpdatedOrAdded += FlightUpdateOrAdded;
            queue.OnChanged += UpdatedEnqueue;
        }

        private void FlightUpdateOrAdded(AMSFlight obj)
        {
            //The subclass Enqueue also fires an event to initiate the transfer
            queue.Enqueue(obj);

        }

        private void TaskCallBack(object state)
        {
            Tuple<Subscription, AMSFlight> info = state as Tuple<Subscription, AMSFlight>;

            Subscription sub = info.Item1;
            AMSFlight flight = info.Item2;

            if (!sub.IsEnabled || sub.ValidUntil < DateTime.Now) return;

            //Enqueue the new flight, and then process the Backlog queue until empty
            if (flight != null) sub.BackLog.Enqueue(flight);

            foreach (AMSFlight fl in sub.BackLog)
            {
                if (!sub.IsArrival && flight.flightId.flightkind.ToLower() == "arrival")
                {
                    Console.WriteLine("Arrival Check Failure");
                    continue;
                }
                if (!sub.IsDeparture && flight.flightId.flightkind.ToLower() == "departure")
                {
                    Console.WriteLine("Departure Check Failure");
                    continue;
                }
                if (sub.AirportIATA != null)
                {
                    if (sub.AirportIATA != flight.flightId.iatalocalairport)
                    {
                        Console.WriteLine("Airport Check Failure");
                        continue;
                    }
                }
                if (sub.AirlineIATA != null)
                {
                    if (sub.AirlineIATA != flight.flightId.iataAirline)
                    {
                        Console.WriteLine("Airline Check Failure");
                        continue;
                    }
                }

                TimeSpan? ts = flight.flightId.scheduleDateTime - DateTime.Now;
                if (ts.Value.TotalHours > sub.MaxHorizonInHours)
                {
                    Console.WriteLine("Max Window Check Failure");
                    continue;
                }
                ts = DateTime.Now - flight.flightId.scheduleDateTime;
                if (ts.Value.TotalHours < sub.MinHorizonInHours)
                {
                    Console.WriteLine("Min Check Failure");
                    continue;
                }

                // Customise for user
                // Adjust the result so only elements the user is allowed to see are set
                List<string> validFields = configService.config.ValidUserFields(sub.SubscriberToken);
                List<string> validCustomFields = configService.config.ValidUserCustomFields(sub.SubscriberToken);
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

                // Made it this far, so OK to try send the message

                string content = flight.XmlRaw;
                string mediaType = "text/xml";

                if (sub.DataFormat.ToLower() == "json")
                {
                    mediaType = "text/json";
                    content = JsonConvert.SerializeObject(flight, Formatting.Indented, new JsonSerializerSettings
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
                                    sub.ConsecutiveUnsuccessfullCalls = 0;
                                }
                                else
                                {
                                    sub.ConsecutiveSuccessfullCalls = 0;
                                    sub.ConsecutiveUnsuccessfullCalls++;
                                    sub.LastFailure = DateTime.Now;
                                    sub.LastError = $"HTTP Status Code: {response.StatusCode}";
                                    sub.BackLog.Enqueue(fl);
                                }
                            }
                            catch (Exception ex)
                            {
                                sub.ConsecutiveSuccessfullCalls = 0;
                                sub.ConsecutiveUnsuccessfullCalls++;
                                sub.LastFailure = DateTime.Now;
                                sub.LastError = ex.Message;
                                sub.BackLog.Enqueue(fl);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sub.ConsecutiveSuccessfullCalls = 0;
                        sub.ConsecutiveUnsuccessfullCalls++;
                        sub.LastFailure = DateTime.Now;
                        sub.LastError = ex.Message;
                        sub.BackLog.Enqueue(fl);
                    }
                }
                else
                {
                    Console.WriteLine(content);
                }

                eventExchange.SubscriptionSend();

            }
        }

        public void Dispose()
        {
            eventExchange.OnFlightUpdatedOrAdded -= FlightUpdateOrAdded;
            queue.OnChanged -= UpdatedEnqueue;
        }

        public string SendBacklog(Subscription s)
        {

            ThreadPool.SetMinThreads(Math.Min(subManager.Subscriptions.Count, 5), 0);
            ThreadPool.SetMaxThreads(Math.Min(subManager.Subscriptions.Count, 40), 0);

            int depth = s.BackLog.Count;
            Tuple<Subscription, AMSFlight> state = new Tuple<Subscription, AMSFlight>(s, null);
            ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCallBack), state);

            return depth.ToString();

        }
        private void UpdatedEnqueue()
        {

            ThreadPool.SetMinThreads(Math.Min(subManager.Subscriptions.Count, 5), 0);
            ThreadPool.SetMaxThreads(Math.Min(subManager.Subscriptions.Count, 40), 0);
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

