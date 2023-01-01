using AMSGetFlights.Model;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace AMSGetFlights.Services
{
    public class SubscriptionManager
    {
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public SubscriptionManager()
        {
            Subscription s = new();
            s.SubscriptionID = "1";
            s.SubscriberName = "default";
            s.SubscriberToken = "default";
            s.IsArrival = true;
            s.IsDeparture = true;
            s.AirportIATA = "DOH";
            s.AirlineIATA = "QF";
            s.IsEnabled = true;
            s.DataFormat = "JSON";
            s.MaxHorizonInHours = 2000;
            s.MinHorizonInHours = -2000;

            Subscriptions.Add(s);

            Subscription s2 = new();
            s2.SubscriptionID = "2";
            s2.SubscriberName = "default";
            s2.SubscriberToken = "default";
            s2.IsArrival = true;
            s2.IsDeparture = true;
            s2.AirportIATA = "DOH";
            s2.IsEnabled = true;
            s2.DataFormat = "XML";
            s2.MaxHorizonInHours = 2000;
            s2.MinHorizonInHours = -2000;

            Subscriptions.Add(s2);
        }

        internal ActionResult<IEnumerable<Subscription>> GetSubscriptionsForUser(string user)
        {
            return Subscriptions.Where(s => s.SubscriberToken == user).ToList();
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
                        return s;
                    } else
                    {
                        s = new Subscription();
                        s.StatusMessage = $"User {userToken} Not Authorized to change subscription {ID}";
                        return s;
                    }
                }
                else
                {
                    s = new Subscription();
                    s.StatusMessage = $"Subscription {ID} Not Found";
                    return s;
                }
            }
            catch (Exception)
            {
                Subscription s = new Subscription();
                s.StatusMessage = $"Subscription {ID} Not Found";
                return s;
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
                        return s;
                    }
                    else
                    {
                        s = new Subscription();
                        s.StatusMessage = $"User {userToken} Not Authorized to change subscription {ID}";
                        return s;
                    }
                }
                else
                {
                    s = new Subscription();
                    s.StatusMessage = $"Subscription {ID} Not Found";
                    return s;
                }
            }
            catch (Exception)
            {
                Subscription s = new Subscription();
                s.StatusMessage = $"Subscription {ID} Not Found";
                return s;
            }
        }

        internal ActionResult<Subscription> UpdateSubscription(Subscription sub, string user)
        {
            throw new NotImplementedException();
        }

        internal ActionResult<Subscription> Subscribe(Subscription sub)
        {
            throw new NotImplementedException();
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
                        return $"Subscription {ID} Deleted";
                    }
                    else
                    {
                        return $"Subscription {ID} Not Deleted (Not Authorized)";
                    }
                }
                else
                {
                    return $"Subscription {ID} Not Found";
                }
            }
            catch (Exception)
            {
                return $"Subscription {ID} Not Found ";
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
                        return s;
                    }
                    else
                    {
                        s = new Subscription();
                        s.StatusMessage = $"User {userToken} Not Authorized to clear backlog of {ID}";
                        return s;
                    }
                }
                else
                {
                    s = new Subscription();
                    s.StatusMessage = $"Subscription {ID} Not Found";
                    return s;
                }
            }
            catch (Exception)
            {
                Subscription s = new Subscription();
                s.StatusMessage = $"Subscription {ID} Not Found";
                return s;
            }
        }

        internal ActionResult<string> SendBacklog(string iD, string user)
        {
            throw new NotImplementedException();
        }
    }
}
