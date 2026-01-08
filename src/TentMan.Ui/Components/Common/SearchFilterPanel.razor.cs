using Microsoft.AspNetCore.Components;

namespace TentMan.Ui.Components.Common;

/// <summary>
/// A reusable search and filter panel component.
/// </summary>
public partial class SearchFilterPanel : ComponentBase
{
    private string _searchText = string.Empty;
    private string _activeFilterValue = string.Empty;

    /// <summary>
    /// The current search text.
    /// </summary>
    [Parameter]
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                SearchTextChanged.InvokeAsync(value);
            }
        }
    }

    /// <summary>
    /// Callback when search text changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> SearchTextChanged { get; set; }

    /// <summary>
    /// Placeholder text for the search field.
    /// </summary>
    [Parameter]
    public string SearchPlaceholder { get; set; } = "Search...";

    /// <summary>
    /// Available filter options.
    /// </summary>
    [Parameter]
    public List<FilterOption> FilterOptions { get; set; } = new();

    /// <summary>
    /// Whether to show the active/deleted filter.
    /// </summary>
    [Parameter]
    public bool ShowActiveFilter { get; set; }

    /// <summary>
    /// The active filter value.
    /// </summary>
    [Parameter]
    public string ActiveFilterValue
    {
        get => _activeFilterValue;
        set
        {
            if (_activeFilterValue != value)
            {
                _activeFilterValue = value;
                ActiveFilterValueChanged.InvokeAsync(value);
                FiltersChanged.InvokeAsync();
            }
        }
    }

    /// <summary>
    /// Callback when active filter value changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> ActiveFilterValueChanged { get; set; }

    /// <summary>
    /// Callback when any filter changes.
    /// </summary>
    [Parameter]
    public EventCallback FiltersChanged { get; set; }

    private int GetFilterColumnWidth()
    {
        var totalFilters = FilterOptions.Count + (ShowActiveFilter ? 1 : 0);
        return totalFilters switch
        {
            1 => 4,
            2 => 3,
            _ => 2
        };
    }

    private async Task OnSearchChanged(string value)
    {
        SearchText = value;
        await FiltersChanged.InvokeAsync();
    }

    private async Task OnFilterValueChanged(FilterOption filter, string? value)
    {
        filter.SelectedValue = value ?? string.Empty;
        await FiltersChanged.InvokeAsync();
    }

    private async Task ClearFilters()
    {
        SearchText = string.Empty;
        ActiveFilterValue = string.Empty;
        foreach (var filter in FilterOptions)
        {
            filter.SelectedValue = string.Empty;
        }
        await FiltersChanged.InvokeAsync();
    }
}

/// <summary>
/// Represents a filter option with multiple choices.
/// </summary>
public class FilterOption
{
    /// <summary>
    /// The label for the filter.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The available options for this filter.
    /// </summary>
    public List<FilterChoice> Options { get; set; } = new();

    /// <summary>
    /// The currently selected value.
    /// </summary>
    public string SelectedValue { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single choice in a filter.
/// </summary>
public class FilterChoice
{
    /// <summary>
    /// The display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
