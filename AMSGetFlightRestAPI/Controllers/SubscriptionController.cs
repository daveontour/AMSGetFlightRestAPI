using AMSGetFlights.Model;
using AMSGetFlights.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AMSGetFlights.Controllers
{

    /*
     * 
     * Class to implement the enpoints for managing subscriptions 
     * 
     * 
     */

    [Route("subscription")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private SubscriptionManager subManager;

        public SubscriptionController(SubscriptionManager subManager)
        {
            this.subManager = subManager;
        }

        [HttpGet("status")]
        public ActionResult<Subscription> GetStatus()
        {
            return subManager.Subscriptions.ElementAt(0);
        }

        [HttpPost("subscribe")]
        public ActionResult<Subscription> Subscribe([FromBody] Subscription sub)
        {
            string user = GetProvidedUser();
            return subManager.Subscribe(sub, user);
        }

        [HttpGet("subscriptions")]
        public ActionResult<IEnumerable<Subscription>> Subscriptions()
        {

            string user = GetProvidedUser();
            if (user == "default" || user == null)
            {
                return new StatusCodeResult(403);
            }

            return subManager.GetSubscriptionsForUser(user);
        }
        [HttpGet("disable/{ID}")]
        public ActionResult<Subscription> DisableSubscription(string ID)
        {
            string user = GetProvidedUser();
            return subManager.DisableSubscription(ID, user);
        }
        [HttpGet("enable/{ID}")]
        public ActionResult<Subscription> EnableSubscription(string ID)
        {
            string user = GetProvidedUser();
            return subManager.EnableSubscription(ID, user);
        }
        [HttpGet("backlogdepth/{ID}")]
        public ActionResult<string> BacklogDepth(string ID)
        {
            string user = GetProvidedUser();
            return subManager.GetBacklogDepth(ID, user);
        }
        [HttpPost("update")]
        public ActionResult<Subscription> UpdateSubscription([FromBody] Subscription sub)
        {
            string user = GetProvidedUser();
            Response.StatusCode = 403;

            return subManager.UpdateSubscription(sub, user);
        }
        [HttpGet("delete/{ID}")]
        public ActionResult<string> DeleteSubscription(string ID)
        {
            string user = GetProvidedUser();
            return subManager.DeleteSubscription(ID, user);
        }
        [HttpGet("clearbacklog/{ID}")]
        public ActionResult<Subscription> DeleteBacklog(string ID)
        {
            string user = GetProvidedUser();
            return subManager.ClearBacklog(ID, user);
        }
        [HttpGet("sendbacklog/{ID}")]
        public ActionResult<string> SendBacklog(string ID)
        {
            string user = GetProvidedUser();
            return subManager.SendBacklog(ID, user);
        }
        private string GetProvidedUser()
        {
            Request.Headers.TryGetValue("Authorization", out StringValues values);

            string providedUser;
            try
            {
                providedUser = values.ElementAt(0);
                providedUser = providedUser.Replace("Bearer", "").Trim();
            }
            catch (Exception)
            {
                providedUser = "default";
            }
            return providedUser;
        }
    }
}
