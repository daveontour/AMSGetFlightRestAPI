// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AMSGetFlights.Model
{

    public class GetFlightQueryObject
    {
        public bool IsSingleFlight
        {
            get
            {
                if (al == null) return false;
                if (schedDate == null) return false;
                if (flt == null) return false;
                return true;
            }
        }
        public string token { get; set; } = "default";
        public string? al
        {
            get
            {
                if (queryParams.ContainsKey("al"))
                {
                    return queryParams["al"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string? schedDate
        {
            get
            {
                if (queryParams.ContainsKey("scheddate"))
                {
                    return queryParams["scheddate"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string? flt
        {
            get
            {
                if (queryParams.ContainsKey("flt"))
                {
                    return queryParams["flt"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string? apt
        {
            get
            {
                if (queryParams.ContainsKey("apt"))
                {
                    return queryParams["apt"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string? type
        {
            get
            {
                if (queryParams.ContainsKey("type"))
                {
                    return queryParams["type"];
                }
                else
                {
                    return "both";
                }
            }
        }
        public string? route
        {
            get
            {
                if (queryParams.ContainsKey("route"))
                {
                    return queryParams["route"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string? callsign
        {
            get
            {
                if (queryParams.ContainsKey("callsign"))
                {
                    return queryParams["callsign"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string? routeICAO
        {
            get
            {
                if (queryParams.ContainsKey("routeicao"))
                {
                    return queryParams["routeicao"];
                }
                else
                {
                    return null;
                }
            }
        }
        public string includeDeletedFlights
        {
            get
            {
                if (queryParams.ContainsKey("includeDeletedFlights"))
                {
                    return queryParams["includeDeletedFlights"];
                }
                else
                {
                    return "false";
                }
            }
        }
        public string partialResults
        {
            get
            {
                if (queryParams.ContainsKey("partialResults"))
                {
                    return queryParams["partialResults"];
                }
                else
                {
                    return "false";
                }
            }
        }
        public DateTime startQuery { get; set; }
        public DateTime endQuery { get; set; }
        public int NumberOfResults { get; set; }

        private Dictionary<string, string> _queryParams;
        public Dictionary<string, string> queryParams
        {
            get { return _queryParams; }
            set
            {
                _queryParams = value;
                SetDates();
            }
        }

        public string Format { get; internal set; }

        public void SetDates()
        {
            if (_queryParams.ContainsKey("from"))
            {
                try
                {
                    int hours = int.Parse(_queryParams["from"]);
                    startQuery = DateTime.Now.AddHours(hours);
                }
                catch (Exception)
                {
                    startQuery = DateTime.MinValue;
                }
            }
            else if (_queryParams.ContainsKey("fromTime"))
            {
                try
                {
                    startQuery = DateTime.Parse(_queryParams["fromTime"]);
                }
                catch (Exception)
                {
                    startQuery = DateTime.MinValue;
                }
            }
            else
            {
                startQuery = DateTime.Now.AddHours(-24);
            }



            if (_queryParams.ContainsKey("to"))
            {
                try
                {
                    int hours = int.Parse(_queryParams["to"]);
                    endQuery = DateTime.Now.AddHours(hours);
                }
                catch (Exception)
                {
                    endQuery = DateTime.MinValue;
                }
            }
            else if (_queryParams.ContainsKey("toTime"))
            {
                try
                {
                    endQuery = DateTime.Parse(_queryParams["toTime"]);
                }
                catch (Exception)
                {
                    endQuery = DateTime.MinValue;
                }
            }
            else
            {
                endQuery = DateTime.Now.AddHours(24);
            }

            if (_queryParams.ContainsKey("scheddate"))
            {
                startQuery = DateTime.Parse(_queryParams["scheddate"]);
                endQuery = DateTime.Parse(_queryParams["scheddate"]).AddDays(1);
            }
        }
        public void SetDefaults(Dictionary<string, string> defaults, Dictionary<string, string> overrides)
        {
            foreach (string key in defaults.Keys)
            {
                if (!queryParams.ContainsKey(key))
                {
                    queryParams.Add(key, defaults[key]);
                }
            }

            foreach (string key in overrides.Keys)
            {
                queryParams[key] = overrides[key];
            }
            SetDates();
        }
    }
}
