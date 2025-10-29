using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Playwright.Axe;
using Xunit.Sdk;
using Microsoft.Playwright;
using PlaywrightClient = Microsoft.Playwright.Playwright;

namespace Archu.Ui.Tests.TestInfrastructure;

/// <summary>
/// Provides accessibility verification helpers that execute axe-core against rendered fragments.
/// </summary>
public static class AccessibilityVerificationExtensions
{
    private static readonly SemaphoreSlim InstallationLock = new(1, 1);
    private static bool _browsersInstalled;

    /// <summary>
    /// Runs an axe-core accessibility scan against the rendered fragment and fails the test when violations occur.
    /// The fragment is isolated inside a dedicated container so document-level rules only evaluate rendered markup.
    /// </summary>
    /// <param name="fragment">The rendered component fragment to validate.</param>
    /// <param name="cancellationToken">An optional cancellation token for the underlying Playwright operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when the fragment is <c>null</c>.</exception>
    public static async Task VerifyAccessibilityAsync(this IRenderedFragment fragment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fragment);

        await EnsurePlaywrightBrowsersInstalledAsync(cancellationToken).ConfigureAwait(false);

        const string componentRootSelector = "#axe-component-under-test";

        using var playwright = await PlaywrightClient.CreateAsync().ConfigureAwait(false);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox" }
        }).ConfigureAwait(false);

        var page = await browser.NewPageAsync().ConfigureAwait(false);
        var htmlDocument = $"<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" /><title>Accessibility Test</title></head><body><div id=\"axe-component-under-test\">{fragment.Markup}</div></body></html>";
        await page.SetContentAsync(htmlDocument).ConfigureAwait(false);

        var results = await page.Locator(componentRootSelector).RunAxe().ConfigureAwait(false);
        if (results.Violations.Count <= 0)
        {
            return;
        }

        var message = BuildViolationMessage(results);
        throw new XunitException(message);
    }

    /// <summary>
    /// Ensures the Playwright Chromium browser is installed so axe scans can execute in a headless environment.
    /// </summary>
    private static async Task EnsurePlaywrightBrowsersInstalledAsync(CancellationToken cancellationToken)
    {
        if (_browsersInstalled)
        {
            return;
        }

        await InstallationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_browsersInstalled)
            {
                return;
            }

            var exitCode = await Task.Run(() => Microsoft.Playwright.Program.Main(new[] { "install", "chromium" }), cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Playwright browser installation failed with exit code {exitCode}.");
            }

            _browsersInstalled = true;
        }
        finally
        {
            InstallationLock.Release();
        }
    }

    /// <summary>
    /// Builds a human readable description of axe-core violations encountered during testing.
    /// </summary>
    private static string BuildViolationMessage(AxeResults results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Accessibility violations detected:");

        foreach (var violation in results.Violations)
        {
            builder.AppendLine($"- {violation.Id}: {violation.Description}");
            foreach (var node in violation.Nodes)
            {
                builder.AppendLine($"  Node: {node.Html}");
                if (!string.IsNullOrWhiteSpace(node.FailureSummary))
                {
                    builder.AppendLine($"    {node.FailureSummary}");
                }
            }
        }

        return builder.ToString();
    }
}
