using Microsoft.Playwright;
using EdgeAnalytics.Abstractions.Extract;
using System.Diagnostics;

public sealed class PlaywrightScraper : IScraper
{
    private static readonly ActivitySource ActivitySource = new("EdgeAnalytics.Scraping");
    
    public async Task<string> FetchAsync(Uri uri, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity(
            "http.fetch",
            ActivityKind.Client);

            activity?.SetTag("http.url", uri.ToString());

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                new() { Headless = true });

            var page = await browser.NewPageAsync();
            await page.GotoAsync(uri.ToString());

            return await page.ContentAsync();
    }
}
