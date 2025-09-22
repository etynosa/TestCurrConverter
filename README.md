# Currency Converter API

## Overview
This is a robust and scalable currency conversion API built with ASP.NET Core 8 and .NET 8. It provides real-time and historical exchange rate data, backed by Entity Framework Core and a SQL Server database.

## Features
- **ASP.NET Core 8**: High-performance, cross-platform framework for building the web API.
- **Entity Framework Core**: ORM for data persistence and management with a SQL Server database.
- **API Versioning**: Supports versioning to ensure non-breaking changes for clients.
- **Resiliency Patterns**: Utilizes Polly for implementing retry policies on external HTTP requests.
- **Background Jobs**: A hosted background service periodically fetches and updates exchange rates.
- **Rate Limiting**: Custom middleware to control API usage and prevent abuse.
- **API Key Authentication**: Secure access to endpoints via a custom API key middleware.
- **Global Exception Handling**: Centralized error management for consistent API responses.
- **Swagger/OpenAPI**: Provides interactive API documentation and testing capabilities.

## Getting Started
### Installation
Follow these steps to get the project running on your local machine.

1.  **Clone the repository**
    ```bash
    git clone https://github.com/etynosa/TestCurrConverter.git
    cd TestCurrConverter
    ```

2.  **Configure Environment Variables**
    Rename `appsettings.json` to `appsettings.Development.json` and update the values with your local configuration.

3.  **Install Dependencies**
    ```bash
    dotnet restore
    ```

4.  **Apply Database Migrations**
    Ensure your SQL Server instance is running and the connection string is correctly configured.
    ```bash
    dotnet ef database update
    ```

5.  **Run the Application**
    ```bash
    dotnet run
    ```
    The API will be available at `https://localhost:7017` and `http://localhost:5199`. The Swagger UI can be accessed at `https://localhost:7017/swagger`.

### Environment Variables
The following variables are required in your `appsettings.Development.json` file.

| Variable                          | Description                                         | Example                                                                                    |
| --------------------------------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `ConnectionStrings.DefaultConnection` | SQL Server connection string.                       | `"Server=(localdb)\\mssqllocaldb;Database=CurrencyApiDb;Trusted_Connection=true;"`          |
| `ExternalService.BaseUrl`         | Base URL of the external exchange rate provider.    | `"https://api.exchangerates.io/"`                                                          |
| `ExternalService.ApiKey`          | API key for the external exchange rate provider.    | `"your-external-service-api-key"`                                                          |
| `ApiKeys`                         | An array of valid API keys for accessing this API.  | `["default-api-key", "test-key-1", "test-key-2"]`                                          |
| `RateLimit.RequestsPerHour`       | Maximum number of requests allowed per hour per key. | `1000`                                                                                     |

## API Documentation
### Base URL
`https://localhost:7017/api/v1`

### Authentication
All endpoints (except `/health`) require an API key to be passed in the `X-API-Key` request header.

### Endpoints
#### POST /currency/convert
Converts an amount from one currency to another using the latest available exchange rate.

**Request**:
```json
{
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "amount": 150.50
}
```

**Response**:
```json
{
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "originalAmount": 150.50,
  "convertedAmount": 139.2125,
  "exchangeRate": 0.925,
  "date": "2024-05-21T12:30:00Z",
  "isRealTime": true
}
```

**Errors**:
- `400 Bad Request`: Invalid request parameters (e.g., missing fields, invalid currency code).
- `401 Unauthorized`: API key is missing or invalid.
- `404 Not Found`: Exchange rate for the requested currency pair is not available.
- `429 Too Many Requests`: API rate limit has been exceeded.
- `500 Internal Server Error`: An unexpected server error occurred.

---

#### POST /currency/convert/historical
Converts an amount from one currency to another using the exchange rate from a specific historical date.

**Request**:
```json
{
  "fromCurrency": "GBP",
  "toCurrency": "JPY",
  "amount": 1000,
  "date": "2023-11-15T00:00:00Z"
}
```

**Response**:
```json
{
  "fromCurrency": "GBP",
  "toCurrency": "JPY",
  "originalAmount": 1000,
  "convertedAmount": 193750.00,
  "exchangeRate": 193.75,
  "date": "2023-11-15T00:00:00Z",
  "isRealTime": false
}
```

**Errors**:
- `400 Bad Request`: Invalid request parameters or date is in the future.
- `401 Unauthorized`: API key is missing or invalid.
- `404 Not Found`: Historical exchange rate for the specified date and pair is not available.
- `429 Too Many Requests`: API rate limit has been exceeded.

---

#### GET /currency/rates/historical
Retrieves a series of historical exchange rates for a currency pair over a specified date range.

**Request**:
`GET /api/v1/currency/rates/historical?BaseCurrency=USD&TargetCurrency=GBP&StartDate=2024-05-01&EndDate=2024-05-03`

**Response**:
```json
{
  "baseCurrency": "USD",
  "targetCurrency": "GBP",
  "rates": {
    "2024-05-01T00:00:00Z": 0.8015,
    "2024-05-02T00:00:00Z": 0.7998,
    "2024-05-03T00:00:00Z": 0.8021
  }
}
```

**Errors**:
- `400 Bad Request`: Invalid request parameters (e.g., start date is after end date).
- `401 Unauthorized`: API key is missing or invalid.
- `429 Too Many Requests`: API rate limit has been exceeded.

---

#### POST /currency/rates/update
Manually triggers the background process to update real-time exchange rates from the external provider.

**Request**:
*(Empty Body)*

**Response**:
```json
{
  "message": "Exchange rates updated successfully"
}
```

**Errors**:
- `401 Unauthorized`: API key is missing or invalid.
- `429 Too Many Requests`: API rate limit has been exceeded.
- `500 Internal Server Error`: An error occurred during the update process.

---

#### GET /health
Checks the health of the API. This endpoint does not require authentication.

**Request**:
`GET /health`

**Response**:
(Status Code: 200 OK)
```
Healthy
```

**Errors**:
- `503 Service Unavailable`: One or more health checks failed.
