using System;
using System.Net.Http;
using System.Net.Http.Json;
using TentMan.Ui.State;
using Microsoft.AspNetCore.Components;

namespace TentMan.Ui.Pages;

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
    /// Gets or sets the shared UI state container that coordinates the busy and error workflow for the page.
    /// </summary>
    [Inject]
    public UiState UiState { get; set; } = default!;

    /// <summary>
    /// Loads the weather forecast data when the component is initialized.
    /// </summary>
    /// <returns>A task that completes after the forecast data has been loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        await LoadForecastsAsync();
    }

    /// <summary>
    /// Fetches the weather forecast data while updating the busy state so the UI can render loading and retry affordances.
    /// </summary>
    /// <returns>A task that completes when the forecast request has succeeded or its error has been recorded.</returns>
    private async Task LoadForecastsAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading forecast data...");
        UiState.Busy.ClearError();

        try
        {
            var result = await Http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json");
            forecasts = result ?? Array.Empty<WeatherForecast>();
            UiState.Busy.ClearError();
        }
        catch (Exception ex)
        {
            UiState.Busy.SetError($"Error loading forecasts: {ex.Message}");
            forecasts = null;
        }
    }

    private sealed class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }

        public int TemperatureF => (int)((TemperatureC * 9.0 / 5.0) + 32);
    }
}
