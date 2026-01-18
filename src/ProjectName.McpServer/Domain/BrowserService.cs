using Microsoft.Playwright;

namespace ProjectName.McpServer.Domain;

public partial class BrowserService(ILogger<BrowserService> logger)
{
    private IBrowser? _browser;

    [LoggerMessage(EventId = 200, Level = LogLevel.Error, Message = "Failed to launch Playwright. Ensure browsers are installed.")]
    private partial void LogBrowserError(Exception ex);

    private async Task EnsureBrowserAsync()
    {
        if (_browser != null) return;

        try
        {
            // Initializes Playwright. 
            // NOTE: In a fresh environment, this might require running the playwright install script.
            var playwright = await Playwright.CreateAsync();
            _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }
        catch (Exception ex)
        {
            LogBrowserError(ex);
            throw new InvalidOperationException("Browser not available. " + ex.Message);
        }
    }

    public async Task<string> ScrapeContentAsync(string url)
    {
        await EnsureBrowserAsync();
        var page = await _browser!.NewPageAsync();

        try
        {
            await page.GotoAsync(url);

            // Wait for network idle to handle ASP.NET Core hydration/loading
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var content = await page.ContentAsync();
            return content;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<string> TakeSnapshotAsync(string url)
    {
        await EnsureBrowserAsync();
        var page = await _browser!.NewPageAsync();
        try
        {
            await page.GotoAsync(url);

            // Wait for network idle here too
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var bytes = await page.ScreenshotAsync();
            return Convert.ToBase64String(bytes);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
