using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Archu.ApiClient.Authentication.Models;
using Archu.ApiClient.Authentication.Services;
using Archu.Ui.Services;
using Archu.Ui.Theming;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using ClientAuthenticationState = Archu.ApiClient.Authentication.Models.AuthenticationState;
using BlazorAuthenticationState = Microsoft.AspNetCore.Components.Authorization.AuthenticationState;
using MudBlazor;
using MudBlazor.Services;

namespace Archu.Ui.Tests.TestInfrastructure;

/// <summary>
/// Provides helpers for creating configured <see cref="TestContext"/> instances shared across UI component tests.
/// </summary>
public static class UiTestContextFactory
{
    /// <summary>
    /// Creates a Bunit <see cref="TestContext"/> that registers MudBlazor services, theme tokens, and axe support.
    /// </summary>
    /// <returns>A disposable <see cref="TestContext"/> ready for rendering Archu UI components.</returns>
    public static TestContext Create()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddAuthorizationCore();
        context.Services.AddSingleton<IAuthorizationService, AllowAllAuthorizationService>();
        context.Services.AddSingleton<IAuthenticationService, NoopAuthenticationService>();
        context.Services.AddSingleton<IThemeTokenService>(new TestThemeTokenService());
        context.Services.AddSingleton<IClientFeatureService>(new TestClientFeatureService());
        var authenticationStateProvider = new FixedAuthenticationStateProvider();
        context.Services.AddSingleton<AuthenticationStateProvider>(authenticationStateProvider);
        context.Services.AddSingleton(authenticationStateProvider);
        return context;
    }

    /// <summary>
    /// Configures the supplied <see cref="TestContext"/> so authentication-aware components behave as if a user is signed in.
    /// </summary>
    /// <param name="context">The test context whose authentication state will be updated.</param>
    /// <param name="userName">The display name applied to the authenticated identity.</param>
    public static void SetAuthenticatedUser(TestContext context, string userName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var provider = context.Services.GetRequiredService<FixedAuthenticationStateProvider>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, authenticationType: "TestAuth");
        provider.SetAuthenticationState(new BlazorAuthenticationState(new ClaimsPrincipal(identity)));
    }

    /// <summary>
    /// Reverts the authentication state to an anonymous identity for the supplied <see cref="TestContext"/>.
    /// </summary>
    /// <param name="context">The test context that should surface an unauthenticated user.</param>
    public static void SetAnonymousUser(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var provider = context.Services.GetRequiredService<FixedAuthenticationStateProvider>();
        provider.SetAuthenticationState(new BlazorAuthenticationState(new ClaimsPrincipal()));
    }

    private sealed class NoopAuthenticationService : IAuthenticationService
    {
        /// <summary>
        /// Returns a failed authentication result because login workflows are not exercised in component accessibility tests.
        /// </summary>
        public Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(AuthenticationResult.Failed("Login is not simulated in tests"));

        /// <summary>
        /// Returns a failed authentication result because registration workflows are not evaluated in these tests.
        /// </summary>
        public Task<AuthenticationResult> RegisterAsync(string email, string password, string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult(AuthenticationResult.Failed("Registration is not simulated in tests"));

        /// <summary>
        /// Completes immediately because logout flows are not triggered during accessibility verification.
        /// </summary>
        public Task LogoutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Returns a failed authentication result because token refresh flows are not under test for accessibility scenarios.
        /// </summary>
        public Task<AuthenticationResult> RefreshTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AuthenticationResult.Failed("Token refresh is not simulated in tests"));

        /// <summary>
        /// Provides an unauthenticated state to satisfy <see cref="AuthorizeView"/> without contacting live authentication services.
        /// </summary>
        public Task<ClientAuthenticationState> GetAuthenticationStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ClientAuthenticationState.Unauthenticated());
    }

    private sealed class TestThemeTokenService : IThemeTokenService
    {
        /// <summary>
        /// Raised when callers want to mimic token changes; rarely used but supplied for interface parity.
        /// </summary>
        public event EventHandler<ThemeTokensChangedEventArgs>? TokensChanged;

        /// <summary>
        /// Returns default design tokens because specific color choices do not impact accessibility assertions under test.
        /// </summary>
        public DesignTokens GetTokens() => new();

        /// <summary>
        /// Returns a default MudBlazor theme so components render without requiring production configuration.
        /// </summary>
        public MudTheme GetMudTheme() => new();

        /// <summary>
        /// Invokes the provided callback and publishes a change notification so layout code paths remain exercised.
        /// </summary>
        public void ApplyOverrides(Action<DesignTokens> configure)
        {
            var tokens = new DesignTokens();
            configure(tokens);
            TokensChanged?.Invoke(this, new ThemeTokensChangedEventArgs(tokens));
        }
    }

    private sealed class TestClientFeatureService : IClientFeatureService
    {
        /// <summary>
        /// Always reports features as disabled because component accessibility exercises do not require flag variations.
        /// </summary>
        /// <param name="featureName">The identifier of the feature flag under evaluation.</param>
        /// <returns>A completed task whose result is <c>false</c> to keep rendering deterministic in tests.</returns>
        public Task<bool> IsEnabledAsync(string featureName) => Task.FromResult(false);
    }

    private sealed class FixedAuthenticationStateProvider : AuthenticationStateProvider
    {
        private BlazorAuthenticationState _state = new(new ClaimsPrincipal());

        /// <summary>
        /// Updates the authentication snapshot and notifies subscribers about the state change.
        /// </summary>
        /// <param name="state">The authentication state that should be exposed to consumers.</param>
        public void SetAuthenticationState(BlazorAuthenticationState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            NotifyAuthenticationStateChanged(Task.FromResult(_state));
        }

        /// <summary>
        /// Returns the most recently configured authentication state so components can evaluate <see cref="AuthorizeView"/> logic.
        /// </summary>
        /// <returns>A task that resolves to the currently active authentication state.</returns>
        public override Task<BlazorAuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    private sealed class AllowAllAuthorizationService : IAuthorizationService
    {
        /// <summary>
        /// Approves all authorization requests irrespective of the supplied requirements.
        /// </summary>
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        /// <summary>
        /// Approves all authorization requests irrespective of the supplied policy name.
        /// </summary>
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }
}
