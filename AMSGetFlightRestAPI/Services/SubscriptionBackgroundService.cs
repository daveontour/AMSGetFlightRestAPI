namespace AMSGetFlights.Services
{
    public class SubscriptionBackgroundService : BackgroundService
    {
        private readonly SubscriptionDispatcher service;

        public SubscriptionBackgroundService(SubscriptionDispatcher service)
        {
            this.service = service;

        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await service.BackgroundProcessing(stoppingToken);
        }
    }
}
