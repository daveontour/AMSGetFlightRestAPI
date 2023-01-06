using AMSGetFlights.Model;
using Newtonsoft.Json;

namespace AMSGetFlights.Services
{
    public class GetFlightsConfig : ICloneable
    {
        public string TryItDefaultURL { get; set; }
        public string TryItDefaultToken { get; set; }
        public string? SQLConnectionString { get; set; }
        public string AdminPass { get; set; } = "admin";
        public bool AllowAMSXFormat { get; set; } = true;
        public bool AllowJSONFormat { get; set; } = true;
        public int BackwardWindowInDays { get; set; } = -1;
        public int ForewardWindowInDays { get; set; } = 2;
        public int ChunkSizeInDays { get; set; } = 1;
        public bool AllowAnnonymousUsers { get; set; } = true;
        public string? RefreshCron { get; set; } = "0 15 2,21 ? * * *";  // Default at 02:15 and 21:15 
        public double UTCOffset { get; set; }
        public string? StorageDirectory { get; set; }
        public string? ConfigurationFile { get; set; }
        public string? Storage { get; set; }
        public bool EnableDirectAMSLookukOnSingleFlightCacheFailure { get; set; } = false;
        public bool EnableDirectAMSLookukOnMultiFlightCacheFailure { get; set; } = false;
        public Dictionary<string, string> MappedQueryParameters { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> CustomFieldToParameter { get; set; } = new Dictionary<string, string>();
        public List<AirportSource> Airports { get; set; } = new List<AirportSource>();
        public Dictionary<string, User> Users { get; set; } = new Dictionary<string, User>();
        public bool IsTest { get; set; } = false;
        public bool EnableSubscriptions { get; set; } = false;  

        public List<string> ValidUserFields(string user)
        {
            if (Users.ContainsKey(user))
            {
                return Users[user].AllowedFields;
            }
            else
            {
                return new List<string>() { };
            }
        }
        public List<string> ValidUserCustomFields(string user)
        {
            if (Users.ContainsKey(user))
            {
                return Users[user].AllowedCustomFields;
            }
            else
            {
                return new List<string>() { };
            }

        }
        public List<AirportSource> GetAirports()
        {
            return Airports;
        }
        public Dictionary<string, string> GetMappedExtras()
        {
            return MappedQueryParameters;
        }
        public object Clone()
        {
            GetFlightsConfig clone = (GetFlightsConfig)MemberwiseClone();
            clone.MappedQueryParameters = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in MappedQueryParameters)
            {
                clone.MappedQueryParameters[kvp.Key] = kvp.Value;
            }
            clone.Users = new Dictionary<string, User>();
            foreach (KeyValuePair<string, User> kvp in Users)
            {
                clone.Users[kvp.Key] = (User)kvp.Value.Clone();
            }
            return clone;
        }
    }
    public class GetFlightsConfigService 
    {
        public string? CurrentConfigFile { get; set; } = null;

        public GetFlightsConfig? config { get; set; }
        public GetFlightsConfigService(IConfiguration env)
        {

            string webConfigFile = env.GetSection("GetFlights").GetValue<string>("ConfigFile");
            config = JsonConvert.DeserializeObject<GetFlightsConfig>(File.ReadAllText(webConfigFile));
            if (config != null)
            {
                config.ConfigurationFile = webConfigFile;
                CurrentConfigFile = webConfigFile;
            }
        }
        public void SaveConfig(GetFlightsConfig? localconfig = null)
        {
            if (localconfig != null && CurrentConfigFile != null)
            {
                File.WriteAllText(CurrentConfigFile, JsonConvert.SerializeObject(localconfig, Formatting.Indented));
            }
            else
            {
                File.WriteAllText(CurrentConfigFile, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
        }
        public void ApplyConfig(GetFlightsConfig? newconfig)
        {
            if (newconfig != null)
            config = (GetFlightsConfig)newconfig.Clone();
        }
    }
}