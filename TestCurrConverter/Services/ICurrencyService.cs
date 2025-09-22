using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using TestCurrConverter.Common;
using TestCurrConverter.Data;
using TestCurrConverter.Data.Dtos;
using TestCurrConverter.Data.Models;

namespace TestCurrConverter.Services
{
    public interface ICurrencyService
    {
        Task<GenericResponseModel<ConversionResponse?>> ConvertCurrencyAsync(ConversionRequest request);
        Task<GenericResponseModel<ConversionResponse?>> ConvertCurrencyHistoricalAsync(HistoricalConversionRequest request);
        Task<GenericResponseModel<HistoricalRateResponse>> GetHistoricalRatesAsync(HistoricalRateRequest request);
        Task UpdateRealTimeRatesAsync();
        Task UpdateHistoricalRatesAsync();
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IExternalCurrencyService _externalService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CurrencyService> _logger;

        public CurrencyService(ApplicationDbContext context, IExternalCurrencyService externalService,
            IConfiguration configuration, ILogger<CurrencyService> logger)
        {
            _context = context;
            _externalService = externalService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GenericResponseModel<ConversionResponse?>> ConvertCurrencyAsync(ConversionRequest request)
        {
            var rate = await GetLatestExchangeRateAsync(request.FromCurrency, request.ToCurrency);

            if (rate == null)
            {
                await UpdateRealTimeRatesAsync();
                rate = await GetLatestExchangeRateAsync(request.FromCurrency, request.ToCurrency);
            }

            if (rate == null)
            {
                return null;
            }

            var res =  new ConversionResponse
            {
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                OriginalAmount = request.Amount,
                ConvertedAmount = request.Amount * rate.Rate,
                ExchangeRate = rate.Rate,
                Date = rate.Date,
                IsRealTime = rate.IsRealTime
            };

            return new GenericResponseModel<ConversionResponse?>(res, CustomMessages.ConversionMessage, true);
        }

        public async Task<GenericResponseModel<ConversionResponse?>> ConvertCurrencyHistoricalAsync(HistoricalConversionRequest request)
        {
            var rate = await _context.ExchangeRates
                .Where(r => r.BaseCurrency == request.FromCurrency &&
                           r.TargetCurrency == request.ToCurrency &&
                           r.Date.Date == request.Date.Date)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (rate == null)
            {
                return null;
            }

            var res = new ConversionResponse
            {
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                OriginalAmount = request.Amount,
                ConvertedAmount = request.Amount * rate.Rate,
                ExchangeRate = rate.Rate,
                Date = rate.Date,
                IsRealTime = rate.IsRealTime
            };

            return new GenericResponseModel<ConversionResponse?>(res, CustomMessages.ConversionMessage, true);
        }

        public async Task<GenericResponseModel<HistoricalRateResponse>> GetHistoricalRatesAsync(HistoricalRateRequest request)
        {
            var rates = await _context.ExchangeRates
                .Where(r => r.BaseCurrency == request.BaseCurrency &&
                           r.TargetCurrency == request.TargetCurrency &&
                           r.Date.Date >= request.StartDate.Date &&
                           r.Date.Date <= request.EndDate.Date)
                .OrderBy(r => r.Date)
                .ToListAsync();

            var ratesDictionary = rates
                .GroupBy(r => r.Date.Date)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedDate).First().Rate);

            var response = new HistoricalRateResponse
            {
                BaseCurrency = request.BaseCurrency,
                TargetCurrency = request.TargetCurrency,
                Rates = ratesDictionary
            };

            return new GenericResponseModel<HistoricalRateResponse>(response, CustomMessages.HistoricalRateMessage, true);
        }

        public async Task UpdateRealTimeRatesAsync()
        {
            var apiKey = _configuration["ExternalService:ApiKey"] ?? "default-api-key";
            var baseCurrencies = new[] { "USD", "EUR", "GBP" };

            foreach (var baseCurrency in baseCurrencies)
            {
                try
                {
                    var response = await _externalService.GetRealTimeRatesAsync(baseCurrency, apiKey);
                    if (response != null)
                    {
                        await StoreRealTimeRatesAsync(response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update real-time rates for {BaseCurrency}", baseCurrency);
                }
            }
        }

        public async Task UpdateHistoricalRatesAsync()
        {
            var apiKey = _configuration["ExternalService:ApiKey"] ?? "default-api-key";
            var pairs = await _context.CurrencyPairs.Where(p => p.IsActive).ToListAsync();
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-7); // Fetch last 7 days

            foreach (var pair in pairs)
            {
                var response = await _externalService.GetHistoricalRatesAsync(
                        new HistoricalRateRequest
                        {
                            BaseCurrency = pair.BaseCurrency,
                            TargetCurrency = pair.TargetCurrency,
                            StartDate = startDate,
                            EndDate = endDate
                        },
                        apiKey);

                if (response != null)
                {
                    await StoreHistoricalRatesAsync(response);
                }

            }
        }

        private async Task<ExchangeRate?> GetLatestExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            return await _context.ExchangeRates
                .Where(r => r.BaseCurrency == fromCurrency && r.TargetCurrency == toCurrency)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();
        }

        private async Task StoreRealTimeRatesAsync(ExternalRealTimeRateResponse response)
        {
            var date = DateTime.Parse(response.Date);

            foreach (var rate in response.Rates)
            {
                var existingRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(r => r.BaseCurrency == response.Base &&
                                            r.TargetCurrency == rate.Key &&
                                            r.Date.Date == date.Date &&
                                            r.IsRealTime);

                if (existingRate != null)
                {
                    existingRate.Rate = rate.Value;
                    existingRate.CreatedDate = DateTimeOffset.UtcNow;
                }
                else
                {
                    _context.ExchangeRates.Add(new ExchangeRate
                    {
                        BaseCurrency = response.Base,
                        TargetCurrency = rate.Key,
                        Rate = rate.Value,
                        Date = date,
                        IsRealTime = true
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task StoreHistoricalRatesAsync(ExternalHistoricalRateResponse response)
        {
            foreach (var rate in response.Rates)
            {
                var date = DateTime.Parse(rate.Key);

                var existingRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(r => r.BaseCurrency == response.Base &&
                                            r.TargetCurrency == response.Target &&
                                            r.Date.Date == date.Date &&
                                            !r.IsRealTime);

                if (existingRate == null)
                {
                    _context.ExchangeRates.Add(new ExchangeRate
                    {
                        BaseCurrency = response.Base,
                        TargetCurrency = response.Target,
                        Rate = rate.Value,
                        Date = date,
                        IsRealTime = false
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }

}
