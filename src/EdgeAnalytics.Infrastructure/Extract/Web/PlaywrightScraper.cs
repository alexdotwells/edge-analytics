using Microsoft.Playwright;
using EdgeAnalytics.Abstractions.Extract;

public sealed class PlaywrightScraper : IScraper
{
    public async Task<string> FetchAsync(Uri uri, CancellationToken ct)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new() { Headless = true });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(uri.ToString());

        return await page.ContentAsync();
    }
}
