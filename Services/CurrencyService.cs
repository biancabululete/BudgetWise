using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetWise.Services;

public sealed record CurrencyRate(decimal Rate, string Date, bool IsCached);

public sealed class CurrencyService
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "RON", "EUR", "USD"
    };

    private readonly HttpClient _httpClient;

    public CurrencyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CurrencyRate> GetRateAsync(
        string from,
        string to,
        bool allowCached = true,
        CancellationToken cancellationToken = default)
    {
        from = from.Trim().ToUpperInvariant();
        to = to.Trim().ToUpperInvariant();

        if (!SupportedCurrencies.Contains(from) || !SupportedCurrencies.Contains(to))
            throw new ArgumentException("Only RON, EUR and USD are supported.");

        if (from == to)
            return new CurrencyRate(1m, string.Empty, false);

        string cacheKey = $"currency_{from}_{to}";
        try
        {
            string relativeUri = $"v2/rate/{from.ToLowerInvariant()}/{to.ToLowerInvariant()}";
            using HttpResponseMessage response = await _httpClient.GetAsync(relativeUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            FrankfurterRateResponse? data = await JsonSerializer.DeserializeAsync<FrankfurterRateResponse>(
                stream,
                cancellationToken: cancellationToken);

            if (data is null || data.Rate <= 0 || string.IsNullOrWhiteSpace(data.Date) ||
                !string.Equals(data.Base, from, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(data.Quote, to, StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException("The exchange-rate response was invalid.");
            }

            Preferences.Default.Set(
                cacheKey + "_rate",
                data.Rate.ToString(CultureInfo.InvariantCulture));
            Preferences.Default.Set(cacheKey + "_date", data.Date);

            return new CurrencyRate(data.Rate, data.Date, false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (allowCached)
            {
                string savedRate = Preferences.Default.Get(cacheKey + "_rate", string.Empty);
                string savedDate = Preferences.Default.Get(cacheKey + "_date", string.Empty);

                if (decimal.TryParse(savedRate, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate) &&
                    rate > 0 && !string.IsNullOrWhiteSpace(savedDate))
                {
                    return new CurrencyRate(rate, savedDate, true);
                }
            }

            throw new InvalidOperationException(
                "Could not retrieve an exchange rate. Check your internet connection and try again.",
                ex);
        }
    }

    private sealed class FrankfurterRateResponse
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("quote")]
        public string Quote { get; set; } = string.Empty;

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }
    }
}
