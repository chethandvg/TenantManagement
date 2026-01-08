using TentMan.ApiClient.Authentication.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TentMan.Ui.Pages;

/// <summary>
/// Registration page logic that orchestrates account creation and feedback messages.
/// </summary>
public partial class Register : ComponentBase
{
    private readonly RegisterModel model = new();
    private bool isLoading;
    private string? errorMessage;

    /// <summary>
    /// Gets or sets the authentication service responsible for creating new accounts.
    /// </summary>
    [Inject]
    public IAuthenticationService AuthService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager used for redirects after successful registration.
    /// </summary>
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service for surfacing success or error notifications.
    /// </summary>
    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Submits the registration form, validating passwords and displaying success or error notifications.
    /// </summary>
    /// <remarks>
    /// Blocks submission when the confirmation password does not match and, after calling the API, shows either a
    /// success snackbar with a redirect to the login page or presents detailed error information so the user can adjust inputs.
    /// </remarks>
    /// <returns>A task that completes when the registration attempt has been processed.</returns>
    private async Task HandleRegisterAsync()
    {
        if (model.Password != model.ConfirmPassword)
        {
            errorMessage = "Passwords do not match.";
            return;
        }

        isLoading = true;
        errorMessage = null;

        try
        {
            var result = await AuthService.RegisterAsync(model.Email, model.Password, model.UserName);

            if (result.Success)
            {
                Snackbar.Add("Registration successful! Please login.", Severity.Success);
                Navigation.NavigateTo("/login");
            }
            else
            {
                errorMessage = result.ErrorMessage ?? "Registration failed. Please try again.";
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

    private sealed class RegisterModel
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
