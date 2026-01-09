using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.ApiClient.Authentication.Services;
using TentMan.Contracts.TenantInvites;

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

    [Inject]
    public ITenantInvitesApiClient TenantInvitesClient { get; set; } = default!;

    [Inject]
    public IAuthenticationService AuthService { get; set; } = default!;

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
            var response = await TenantInvitesClient.ValidateInviteAsync(_inviteToken!);
            
            if (!response.Success || response.Data == null)
            {
                _isValid = false;
                _errorMessage = response.Message ?? "Failed to validate invite token";
                return;
            }

            var validationResult = response.Data;

            if (!validationResult.IsValid)
            {
                _isValid = false;
                _errorMessage = validationResult.ErrorMessage ?? "This invite link is not valid";
                return;
            }

            // Invite is valid, populate form
            _isValid = true;
            _tenantFullName = validationResult.TenantFullName;
            _inviteEmail = validationResult.Email;
            
            // Pre-fill email if available
            if (!string.IsNullOrWhiteSpace(_inviteEmail))
            {
                _model.Email = _inviteEmail;
            }
        }
        catch (Exception ex)
        {
            _isValid = false;
            _errorMessage = $"Error validating invite: {ex.Message}";
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

        // Client-side validation as UX improvement (server validates too)
        if (_model.Password.Length < 8)
        {
            _submitError = "Password must be at least 8 characters";
            return;
        }

        _isSubmitting = true;
        _submitError = null;

        try
        {
            var request = new AcceptInviteRequest
            {
                InviteToken = _inviteToken!,
                UserName = _model.UserName,
                Email = _model.Email,
                Password = _model.Password
            };

            var response = await TenantInvitesClient.AcceptInviteAsync(request);

            if (!response.Success || response.Data == null)
            {
                _submitError = response.Message ?? "Failed to accept invite. Please try again.";
                return;
            }

            // Successfully accepted invite and received authentication tokens
            Snackbar.Add("Account created successfully! Logging you in...", Severity.Success);
            
            // Redirect to tenant dashboard with force load to ensure authentication state is refreshed
            await Task.Delay(500);
            Navigation.NavigateTo("/tenant/dashboard", forceLoad: true);
        }
        catch (Exception ex)
        {
            _submitError = $"An error occurred: {ex.Message}";
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
