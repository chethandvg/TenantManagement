using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Ui.Pages.Billing;
using TentMan.Ui.State;

namespace TentMan.Ui.Pages.Tenant;

/// <summary>
/// Tenant invoices page (My Bills) with filtering and pagination.
/// </summary>
public partial class TenantInvoices : ComponentBase
{
    private IEnumerable<InvoiceDto>? _invoices;
    private IEnumerable<InvoiceDto> _paginatedInvoices = Enumerable.Empty<InvoiceDto>();
    private InvoiceStatus? _statusFilter;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalPages = 0;

    [Inject]
    public ITenantPortalApiClient TenantPortalClient { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public UiState UiState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoicesAsync();
    }

    private async Task LoadInvoicesAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading bills...");
        UiState.Busy.ClearError();
        _invoices = null;
        _paginatedInvoices = Enumerable.Empty<InvoiceDto>();

        try
        {
            var response = await TenantPortalClient.GetInvoicesAsync(_statusFilter);

            if (response.Success && response.Data != null)
            {
                _invoices = response.Data.OrderByDescending(i => i.InvoiceDate);
                UpdatePagination();
                UiState.Busy.ClearError();
            }
            else
            {
                var message = response.Message ?? "Failed to load bills.";
                UiState.Busy.SetError(message);
                Snackbar.Add(message, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading bills: {ex.Message}";
            UiState.Busy.SetError(message);
            Snackbar.Add(message, Severity.Error);
        }
    }

    private async Task ApplyFiltersAsync()
    {
        _currentPage = 1;
        await LoadInvoicesAsync();
    }

    private void UpdatePagination()
    {
        if (_invoices == null || !_invoices.Any())
        {
            _totalPages = 0;
            _paginatedInvoices = Enumerable.Empty<InvoiceDto>();
            return;
        }

        _totalPages = (int)Math.Ceiling(_invoices.Count() / (double)_pageSize);
        _paginatedInvoices = _invoices.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);
    }

    private void OnPageChanged(int page)
    {
        _currentPage = page;
        UpdatePagination();
    }

    private RenderFragment GetStatusChip(InvoiceStatus status)
    {
        return BillingStatusHelper.GetInvoiceStatusChip(status);
    }
}
