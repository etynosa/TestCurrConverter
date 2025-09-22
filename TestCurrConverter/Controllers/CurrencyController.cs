using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TestCurrConverter.Data.Dtos;
using TestCurrConverter.Services;

namespace TestCurrConverter.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v1/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        [HttpPost("convert")]
        [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConvertCurrency([FromBody] ConversionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Invalid request parameters",
                    StatusCode = 400
                });
            }

                var result = await _currencyService.ConvertCurrencyAsync(request);

                if (result == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Error = "ExchangeRateNotFound",
                        Message = $"Exchange rate not found for {request.FromCurrency} to {request.ToCurrency}",
                        StatusCode = 404
                    });
                }

                return Ok(result);
        }

        [HttpPost("convert/historical")]
        [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConversionResponse>> ConvertCurrencyHistorical([FromBody] HistoricalConversionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Invalid request parameters",
                    StatusCode = 400
                });
            }

            if (request.Date > DateTime.UtcNow.Date)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "InvalidDate",
                    Message = "Date cannot be in the future",
                    StatusCode = 400
                });
            }


            var result = await _currencyService.ConvertCurrencyHistoricalAsync(request);

            if (result == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    Error = "HistoricalRateNotFound",
                    Message = $"Historical exchange rate not found for {request.FromCurrency} to {request.ToCurrency} on {request.Date:yyyy-MM-dd}",
                    StatusCode = 404
                });
            }

            return Ok(result);

        }

        [HttpGet("rates/historical")]
        [ProducesResponseType(typeof(HistoricalRateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHistoricalRates([FromQuery] HistoricalRateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Invalid request parameters",
                    StatusCode = 400
                });
            }

            if (request.StartDate > request.EndDate)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "InvalidDateRange",
                    Message = "Start date must be before or equal to end date",
                    StatusCode = 400
                });
            }

            if (request.EndDate > DateTime.UtcNow.Date)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Error = "InvalidDate",
                    Message = "End date cannot be in the future",
                    StatusCode = 400
                });
            }

            var result = await _currencyService.GetHistoricalRatesAsync(request);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Message);
        }

        [HttpPost("rates/update")]
        public async Task<ActionResult> UpdateRates()
        {
            await _currencyService.UpdateRealTimeRatesAsync();
            return Ok(new { message = "Exchange rates updated successfully" });
        }
    }
}
