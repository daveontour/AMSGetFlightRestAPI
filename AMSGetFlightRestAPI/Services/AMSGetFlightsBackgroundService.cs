namespace AMSGetFlights.Services
{
    public class AMSGetFlightsBackgroundService : BackgroundService
    {
        private readonly AMSGetFlightsStatusService service;

        public AMSGetFlightsBackgroundService(AMSGetFlightsStatusService service)
        {
            this.service = service;

        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await service.BackgroundProcessing(stoppingToken);
        }
    }
}
