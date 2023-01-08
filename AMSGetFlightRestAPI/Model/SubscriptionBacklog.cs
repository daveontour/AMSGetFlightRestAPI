using AMSGetFlights.Services;
using Newtonsoft.Json;

namespace AMSGetFlights.Model
{
    public class SubscriptionBacklog
    {
        public int Count {
            get {
                return _flights.Count;    
                } 
             }
        public int maxDepth = 10000;
        private Queue<string> _flights = new Queue<string>();
        private string BacklogFile;
       
 
        public void Put(AMSFlight? fl)
        {
            if (fl == null)
            {
                return; 
            }
            // Put the flight on the queue and save the queue to disk
            _flights.Enqueue(fl.XmlRaw);
            
            while(Count > maxDepth)
            {
                _flights.TryDequeue(out string discard);
            }

            string json = JsonConvert.SerializeObject(_flights);
            File.WriteAllText(BacklogFile, json);
        }
        public void Clear()
        {
            _flights.Clear();
            string json = JsonConvert.SerializeObject(_flights);
            File.WriteAllText(BacklogFile, json);
        }
        public AMSFlight? Next(GetFlightsConfig config)
        {
            //Take a flight off the Queue, Deserialize it and save the modified queue.
            _flights.TryDequeue(out string flstr);
            if (flstr == null)
            {
                return null;
            }
            AMSFlight fl = new AMSFlight(flstr,config);

            string json = JsonConvert.SerializeObject(_flights);
            File.WriteAllText(BacklogFile, json);

            return fl;
        }

        internal void SetConfig(GetFlightsConfig? config, string? subscriptionID, string? subscriberToken)
        {
            BacklogFile = $"{config.StorageDirectory}/Backlog_{subscriberToken}_{subscriptionID}.json";
            maxDepth = config.BacklogMaxDepth;
        }
    }
}
