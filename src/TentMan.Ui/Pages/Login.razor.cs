using TentMan.ApiClient.Authentication.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TentMan.Ui.Pages;

/// <summary>
/// Login page logic responsible for validating credentials and notifying the user about authentication outcomes.
/// </summary>
public partial class Login : ComponentBase
{
    private readonly LoginModel model = new();
    private bool isLoading;
    private string? errorMessage;

    /// <summary>
    /// Gets or sets the authentication service used to authenticate users.
    /// </summary>
    [Inject]
    public IAuthenticationService AuthService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager for redirecting the user after authentication.
    /// </summary>
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service used to surface feedback messages to the user.
    /// </summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Gets or sets the return URL supplied in the query string when the login page is invoked.
    /// </summary>
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Attempts to authenticate the user, showing success or error feedback depending on the outcome.
    /// </summary>
    /// <remarks>
    /// Shows a success snackbar and redirects when authentication succeeds; otherwise displays a validation
    /// message or surfaces unexpected errors in the alert area so that the user can retry.
    /// </remarks>
    /// <returns>A task that completes when the authentication workflow has finished.</returns>
    private async Task HandleLoginAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var result = await AuthService.LoginAsync(model.Email, model.Password);

            if (result.Success)
            {
                Snackbar.Add("Login successful!", Severity.Success);
                Navigation.NavigateTo(ReturnUrl ?? "/", forceLoad: true);
            }
            else
            {
                errorMessage = result.ErrorMessage ?? "Login failed. Please check your credentials.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private sealed class LoginModel
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
