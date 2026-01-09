using Microsoft.AspNetCore.Components;
using MudBlazor;
using TentMan.ApiClient.Services;
using TentMan.ApiClient.Authentication.Services;
using TentMan.ApiClient.Authentication.Providers;
using TentMan.ApiClient.Authentication.Models;
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
    public ITokenManager TokenManager { get; set; } = default!;

    [Inject]
    public ApiAuthenticationStateProvider? AuthStateProvider { get; set; }

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
            var response = await TenantInvitesClient.ValidateInviteAsync(_inviteToken!, default);
            
            if (response.Success && response.Data != null)
            {
                if (response.Data.IsValid)
                {
                    _isValid = true;
                    _tenantFullName = response.Data.TenantFullName;
                    _inviteEmail = response.Data.Email;
                    
                    // Pre-fill email if available
                    if (!string.IsNullOrWhiteSpace(_inviteEmail))
                    {
                        _model.Email = _inviteEmail;
                    }
                }
                else
                {
                    _isValid = false;
                    _errorMessage = response.Data.ErrorMessage ?? "This invite is no longer valid";
                }
            }
            else
            {
                _isValid = false;
                _errorMessage = response.Message ?? "Failed to validate invite";
            }
        }
        catch (Exception ex)
        {
            _isValid = false;
            _errorMessage = $"An error occurred while validating the invite: {ex.Message}";
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

            var response = await TenantInvitesClient.AcceptInviteAsync(request, default);

            if (response.Success && response.Data != null)
            {
                var authResponse = response.Data;

                // Store the tokens (same pattern as AuthenticationService)
                var tokenResponse = new TokenResponse
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresIn = authResponse.ExpiresIn
                };

                await TokenManager.StoreTokenAsync(tokenResponse, default);

                // Get authentication state from stored token
                var authState = await TokenManager.GetAuthenticationStateAsync(default);

                // Notify the authentication state provider
                if (AuthStateProvider != null)
                {
                    await AuthStateProvider.MarkUserAsAuthenticatedAsync(authState.UserName ?? _model.Email);
                }

                Snackbar.Add("Account created successfully! Welcome to TentMan!", Severity.Success);
                
                // Redirect to tenant dashboard
                Navigation.NavigateTo("/tenant/dashboard", forceLoad: true);
            }
            else
            {
                _submitError = response.Message ?? "Failed to create account. Please try again.";
            }
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
