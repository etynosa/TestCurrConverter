namespace TestCurrConverter.Services
{
    public class BackgroundUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundUpdateService> _logger;

        public BackgroundUpdateService(IServiceProvider serviceProvider, ILogger<BackgroundUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                using var scope = _serviceProvider.CreateScope();
                var currencyService = scope.ServiceProvider.GetRequiredService<ICurrencyService>();

                _logger.LogInformation("Updating real-time exchange rates");
                await currencyService.UpdateRealTimeRatesAsync();


                if (DateTime.UtcNow.Hour == 2) // Run at 2 AM UTC
                {
                    _logger.LogInformation("Updating historical exchange rates");
                    await currencyService.UpdateHistoricalRatesAsync();
                }
                // Wait for 15 minutes before next update
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
