using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TentMan.Ui.Pages.Tenant;

public partial class AcceptInvite : ComponentBase
{
    private readonly AcceptInviteModel _model = new();
    private bool _isValidating = true;
    private bool _isValid;
    private string? _errorMessage;
    private string? _tenantFullName;
    private string? _inviteEmail;
    private bool _isSubmitting;
    private string? _submitError;
    private string? _inviteToken;

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _inviteToken = Token;

        if (string.IsNullOrWhiteSpace(_inviteToken))
        {
            _isValidating = false;
            _isValid = false;
            _errorMessage = "No invite token provided";
            return;
        }

        await ValidateInviteTokenAsync();
    }

    private async Task ValidateInviteTokenAsync()
    {
        try
        {
            // TODO: Call API to validate invite token
            // For now, simulate validation
            await Task.Delay(1000);
            
            // Mock validation success
            _isValid = true;
            _tenantFullName = "John Doe"; // From API response
            _inviteEmail = "john.doe@example.com"; // From API response
            _model.Email = _inviteEmail ?? "";
        }
        catch (Exception ex)
        {
            _isValid = false;
            _errorMessage = ex.Message;
        }
        finally
        {
            _isValidating = false;
        }
    }

    private async Task HandleAcceptInviteAsync()
    {
        if (_model.Password != _model.ConfirmPassword)
        {
            _submitError = "Passwords do not match";
            return;
        }

        if (_model.Password.Length < 8)
        {
            _submitError = "Password must be at least 8 characters";
            return;
        }

        _isSubmitting = true;
        _submitError = null;

        try
        {
            // TODO: Call API to accept invite and create user
            await Task.Delay(1000);

            Snackbar.Add("Account created successfully! Logging you in...", Severity.Success);
            
            // Redirect to tenant dashboard
            await Task.Delay(500);
            Navigation.NavigateTo("/tenant/dashboard", forceLoad: true);
        }
        catch (Exception ex)
        {
            _submitError = ex.Message;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private sealed class AcceptInviteModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
