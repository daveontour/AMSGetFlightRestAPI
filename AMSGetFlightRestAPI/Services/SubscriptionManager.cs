using AMSGetFlights.Model;
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
    }
}
