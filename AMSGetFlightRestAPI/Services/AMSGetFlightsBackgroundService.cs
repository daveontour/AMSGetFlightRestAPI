namespace AMSGetFlights.Services
{
    public class AMSGetFlightsBackgroundService : BackgroundService
    {
        private readonly IAMSGetFlightStatusService service;

        public AMSGetFlightsBackgroundService(IAMSGetFlightStatusService service)
        {
            this.service = service;

        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await service.BackgroundProcessing(stoppingToken);
        }
    }
}
