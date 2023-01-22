using AMSGetFlights.Model;
using Microsoft.AspNetCore.Mvc;


namespace AMSGetFlights.Services;


/*
 *   The SubscriptionController passes all the work to manage the various 
 *   requests to this class. 
 */
public class SubscriptionManager
{
    private readonly GetFlightsConfigService configService;
    private readonly FlightRepository repo;
    private readonly EventExchange eventExchange;

    public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public SubscriptionManager(FlightRepository repo, EventExchange eventExchange, GetFlightsConfigService configService)
    {
        this.configService = configService;
        this.repo = repo;
        this.eventExchange = eventExchange;
        if (configService.config.EnableSubscriptions)
        {
            LoadSubscriptions();
        }
    }

    private void LoadSubscriptions()
    {
        Subscriptions = repo.GetAllSubscriptions();
        foreach (var subscription in Subscriptions)
        {
            subscription.SetConfig(configService.config);
        }
        SaveSubscriptions();
    }
    public void SaveSubscriptions()
    {
        repo.SaveSubsciptions(Subscriptions);
        eventExchange.SubscriptionsChanged(Subscriptions);
    }

    internal ActionResult<IEnumerable<Subscription>> GetSubscriptionsForUser(string user)
    {
        if (!configService.config.EnableSubscriptions)
        {
            return new List<Subscription>() { new Subscription() { StatusMessage = $"Subscription Service Not Enabled", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 } };
        }
        try
        {
            return Subscriptions.Where(s => s.SubscriberToken == user).ToList();
        }
        catch (Exception)
        {
            List<Subscription> subs = new()
            {
                new Subscription() { StatusMessage = $"Error. No subscriptions found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 }
            };
            return subs;
        }
    }
    internal ActionResult<Subscription> DisableSubscription(string ID, string userToken)
    {

        if (!configService.config.EnableSubscriptions)
        {
            return new Subscription() { StatusMessage = $"Subscription Service Not Enabled", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
        }
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
    internal ActionResult<Subscription> EnableSubscription(string ID, string userToken)
    {
        if (!configService.config.EnableSubscriptions)
        {
            return new Subscription() { StatusMessage = $"Subscription Service Not Enabled", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
        }
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
        if (!configService.config.EnableSubscriptions)
        {
            return new Subscription() { StatusMessage = $"Subscription Service Not Enabled", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
        }
        Subscription s = Subscriptions.Where(s => s.SubscriptionID == sub.SubscriptionID)?.First();
        if (s is null)
        {
            return new Subscription() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID} not found", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
        }
        if (s.SubscriberToken != userToken)
        {
            return new Subscription() { StatusMessage = $"Error. User not authorised to make changes", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
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
        if (!configService.config.EnableSubscriptions)
        {
            return new Subscription() { StatusMessage = $"Subscription Service Not Enabled", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
        }
        if (userToken == null)
        {
            return new StatusCodeResult(403);
        }

        try
        {
            Subscription s = Subscriptions.Where(s => s.SubscriptionID == sub.SubscriptionID)?.First();
            if (s is not null)
            {
                Subscription newsub = new() { StatusMessage = $"Error. Subscription ID {sub.SubscriptionID}. Already Exists", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
                newsub.SetConfig(configService.config);
                return newsub;
            }
        }
        catch (Exception)
        {
            // NO-OP
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


        if (sub.DataFormat.ToLower() != "json" && sub.DataFormat.ToLower() != "xml")
        {
            sub.DataFormat = "JSON";
        }


        sub.SubscriberToken = userToken;
        Subscriptions.Add(sub);

        sub.StatusMessage = "Success";
        SaveSubscriptions();

        return sub;
    }
    internal ActionResult<string> DeleteSubscription(string ID, string userToken)
    {
        if (!configService.config.EnableSubscriptions)
        {
            return $"Subscription Service Not Enabled";
        }

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
                    return $"Error. User not authorised to delete subscription";
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
        if (!configService.config.EnableSubscriptions)
        {
            return new Subscription() { StatusMessage = $"Subscription Service Not Enabled", IsArrival = false, IsDeparture = false, IsEnabled = false, ValidUntil = DateTime.MinValue, MaxHorizonInHours = 0, MinHorizonInHours = 0 };
        }
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
        if (!configService.config.EnableSubscriptions)
        {
            return $"Subscription Service Not Enabled";
        }
        try
        {
            Subscription s = Subscriptions.Where(s => s.SubscriptionID == ID)?.First();
            if (s != null)
            {
                if (s.SubscriberToken == userToken)
                {
                    eventExchange.SendBacklog(s);
                    return $"Success.";
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
        if (!configService.config.EnableSubscriptions)
        {
            return $"Subscription Service Not Enabled";
        }
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
