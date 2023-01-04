using AMSGetFlights.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace AMSGetFlights.Services
{
    public class SubscriptionManager
    {
        private IFlightRepository repo;
        private SubscriptionDispatcher dispatcher;
        private EventExchange eventExchange;

        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public SubscriptionManager(IFlightRepository repo, SubscriptionDispatcher dispatcher, EventExchange eventExchange)
        {
            this.repo = repo;
            this.dispatcher = dispatcher;
            this.eventExchange = eventExchange;
            this.dispatcher.SetSubscriptionManager(this);
            LoadSubscriptions();

        }

        private void LoadSubscriptions()
        {
            Subscriptions = repo.GetAllSubscriptions();
            //Subscription s = new();
            //s.SubscriptionID = "1";
            //s.SubscriberName = "Test";
            //s.SubscriberToken = "1ecf91df-cbc1-4a2d-bf61-5a2dc8ae2e33";
            //s.IsArrival = true;
            //s.IsDeparture = true;
            //s.AirportIATA = "DOH";
            //s.AirlineIATA = "QF";
            //s.IsEnabled = true;
            //s.DataFormat = "JSON";
            //s.MaxHorizonInHours = 2000;
            //s.MinHorizonInHours = -2000;

            //Subscriptions.Add(s);

            //Subscription s2 = new();
            //s2.SubscriptionID = "2";
            //s2.SubscriberName = "default";
            //s2.SubscriberToken = "default";
            //s2.IsArrival = true;
            //s2.IsDeparture = true;
            //s2.AirportIATA = "DOH";
            //s2.IsEnabled = true;
            //s2.DataFormat = "XML";
            //s2.MaxHorizonInHours = 2000;
            //s2.MinHorizonInHours = -2000;

            //Subscriptions.Add(s2);

            SaveSubscriptions();
        }

        private void SaveSubscriptions()
        {
            repo.SaveSubsciptions(Subscriptions);
            eventExchange.SubscriptionsChanged(Subscriptions);
        }

        internal ActionResult<IEnumerable<Subscription>> GetSubscriptionsForUser(string user)
        {
            try
            {
                return Subscriptions.Where(s => s.SubscriberToken == user).ToList();
            } catch (Exception)
            {
                List<Subscription> subs = new List<Subscription>();
                subs.Add(new Subscription() { StatusMessage = $"Error. No subscriptions found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 });
                return subs;
            }
        }
        internal ActionResult<Subscription> DisableSubscription(string ID, string userToken)
        {
            
            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
                if (s != null)
                {
                    if (s.SubscriberToken == userToken)
                    {
                        s.IsEnabled = false;
                        SaveSubscriptions();
                        return s;
                    } else
                    {
                        return new Subscription() { StatusMessage = $"Error. User not authorised to make changes", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                    }
                }
                else
                {
                    return new Subscription() { StatusMessage = $"Error. Subscription ID {ID}. Not Found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                }
            }
            catch (Exception)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {ID}. Not Found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }
        }
        internal ActionResult<Subscription> EnableSubscription(string ID, string userToken)
        {

            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
                if (s != null)
                {
                    if (s.SubscriberToken == userToken)
                    {
                        s.IsEnabled = true;
                        SaveSubscriptions();
                        return s;
                    }
                    else
                    {
                        return new Subscription() { StatusMessage = $"Error. User not authorised to make changes", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                    }
                }
                else
                {
                    return new Subscription() { StatusMessage = $"Error. Subscription ID {ID}. Not Found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                }
            }
            catch (Exception)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {ID}. Not Found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }
        }
        internal ActionResult<Subscription> UpdateSubscription(Subscription sub, string userToken)
        {
            Subscription s = Subscriptions.Where(s => s.SubscriptionID == sub.SubscriptionID)?.First();
            if (s is null)
            {
                return new StatusCodeResult(404);
            }
            if (s.SubscriberToken != userToken)
            {
                return new StatusCodeResult(403);
            }

            if (!sub.IsArrival && !sub.IsDeparture)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. One or both of isArrival or isDepature must be set to true", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }

            if (sub.AirportIATA == null)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. airportIATA must be set to airport code of the desired airport", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }

            if (sub.CallBackURL == null)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. CallBack URL is not set", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }
            if (!Uri.IsWellFormedUriString(sub.CallBackURL, UriKind.Absolute))
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. CallBack URL is not well formed", CallBackURL = sub.CallBackURL, IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }

            s.AirlineIATA = sub.AirlineIATA;
            s.AirportIATA = sub.AirportIATA;
            s.AuthorizationHeaderName = sub.AuthorizationHeaderName;
            s.AuthorizationHeaderValue = sub.AuthorizationHeaderValue;
            s.CallBackURL = sub.CallBackURL;    
            s.DataFormat = sub.DataFormat;
            s.IsDeparture = sub.IsDeparture;
            s.IsArrival = sub.IsArrival;

            SaveSubscriptions();

            return s;
        }
        internal ActionResult<Subscription> Subscribe(Subscription sub, string userToken)
        {
            if (userToken == null)
            {
                return new StatusCodeResult(403);
            }

            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == sub.SubscriptionID)?.First();
                if (s is not null)
                {
                    return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. Already Exists", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                }
            }
            catch (Exception)
            {
                // NO-OP
            }

            if(!sub.IsArrival && !sub.IsDeparture)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. One or both of isArrival or isDepature must be set to true", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }

            if (sub.AirportIATA == null)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. airportIATA must be set to airport code of the desired airport", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }

            if (sub.CallBackURL == null)
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. CallBack URL is not set", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }
            if (!Uri.IsWellFormedUriString(sub.CallBackURL, UriKind.Absolute))
            {
                return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. CallBack URL is not well formed", CallBackURL = sub.CallBackURL, IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
            }

            
            if (sub.DataFormat.ToLower() != "json" && sub.DataFormat.ToLower() != "xml")
            {
                sub.DataFormat = "JSON";
            }

      
            Subscriptions.Add(sub);

            sub.StatusMessage = "Success";
            SaveSubscriptions();

            return sub;
        }      
        internal ActionResult<string> DeleteSubscription(string ID, string userToken)
        {
            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
                if (s != null)
                {
                    if (s.SubscriberToken == userToken)
                    {
                        Subscriptions.Remove(s);

                        SaveSubscriptions();
                        return $"Subscription {ID} Deleted";
                    }
                    else
                    {
                        return $"User not authorised to delete subscription";
                    }
                }
                else
                {
                    return $"SubscriptionID {ID} not found";
                }
            }
            catch (Exception)
            {
                return $"SubscriptionID {ID} not found";
            }
        }
        internal ActionResult<Subscription> ClearBacklog(string ID, string userToken)
        {

            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
                if (s != null)
                {
                    if (s.SubscriberToken == userToken)
                    {
                        s.BackLog.Clear();
                        s.StatusMessage = "Backlog Cleared";
                        SaveSubscriptions();
                        s.StatusMessage = $"Sucess. Backlog cleard for Subscription ID: {ID}";
                        return s;
                    }
                    else
                    {
                        return new Subscription() { StatusMessage = $"Error. User not authorised to make changes", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                    }
                }
                else
                {
                    return new StatusCodeResult(404);
                }
            }
            catch (Exception)
            {
                return new StatusCodeResult(404);
            }
        }
        internal ActionResult<string> SendBacklog(string ID, string userToken)
        {
            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
                if (s != null)
                {
                    if (s.SubscriberToken == userToken)
                    {
                        string depth = dispatcher.SendBacklog(s);
                        return $"Success. {depth} messages to send";
                    }
                    else
                    {
                        return "Not Authorised";
                    }
                }
                else
                {
                    return "Subscription Not Found";
                }
            }
            catch (Exception)
            {
                return "Subscription Not Found";
            }
        }
        internal ActionResult<string> GetBacklogDepth(string ID, string userToken)
        {
            try
            {
                Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
                if (s != null)
                {
                    if (s.SubscriberToken == userToken)
                    {
                        string depth = s.BackLog.Count.ToString();
                        return $"Backlog depth for Subscription ID: {ID} =  {depth} messages";
                    }
                    else
                    {
                        return "Not Authorised";
                    }
                }
                else
                {
                    return "Subscription Not Found";
                }
            }
            catch (Exception)
            {
                return "Subscription Not Found";
            }
        }
    }
}
