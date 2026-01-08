# TentMan.Ui API Documentation

The DocFX configuration in this folder produces API reference material directly from the XML documentation comments that live alongside the Blazor components.

## Generate the Site

1. Restore the dotnet tools defined in `.config/dotnet-tools.json`:
   ```bash
   dotnet tool restore
   ```
2. Run DocFX against this configuration:
   ```bash
   dotnet docfx docs/tentman-ui/docfx.json
   ```
3. Open the generated static site located at `docs/tentman-ui/_site/index.html` in your browser.

DocFX uses the `TentMan.Ui` project file as its metadata source, so any new XML comments added to components, pages, or services will automatically appear in the rendered documentation.
