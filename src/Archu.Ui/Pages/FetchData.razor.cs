using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Archu.Ui.Pages;

/// <summary>
/// Displays weather forecast data retrieved from the server API.
/// </summary>
public partial class FetchData : ComponentBase
{
    private WeatherForecast[]? forecasts;

    /// <summary>
    /// Gets or sets the HTTP client used to retrieve weather forecasts from the API.
    /// </summary>
    [Inject]
    public HttpClient Http { get; set; } = default!;

    /// <summary>
    /// Loads the weather forecast data when the component is initialized.
    /// </summary>
    /// <returns>A task that completes after the forecast data has been loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json");
    }

    private sealed class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }

        public int TemperatureF => (int)((TemperatureC * 9.0 / 5.0) + 32);
    }
}
