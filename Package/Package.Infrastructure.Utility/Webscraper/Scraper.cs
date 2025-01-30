using AngleSharp;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Package.Infrastructure.Utility.Webscraper;

public class Scraper
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> Scrape(string baseUrl)
    {
        var data = await ScrapeWebsiteAsync(baseUrl);

        // Convert the scraped data to JSON
        string jsonOutput = JsonSerializer.Serialize(new
        {
            Documents = FlattenScrapedData(data!)
        }, jsonOptions);

        // Output the JSON
        return jsonOutput;
    }

    // Helper method to flatten scraped data into a single list for AI search
    private static List<object> FlattenScrapedData(ScrapedData root)
    {
        var documents = new List<object>();

        void Traverse(ScrapedData node)
        {
            if (node == null) return;

            documents.Add(new
            {
                url = node.Url,
                content = node.TextContent,
                keywords = node.Keywords // Include keywords in the output
            });

            foreach (var child in node.Links)
            {
                Traverse(child);
            }
        }

        Traverse(root);
        return documents;
    }

    // Define a model for the scraped data
    public class ScrapedData
    {
        public string Url { get; set; } = null!;
        public string TextContent { get; set; } = null!;
        public List<string> Keywords { get; set; } = []; // Add keywords property
        public List<ScrapedData> Links { get; set; } = [];
    }

    // Set to track visited URLs
    private readonly ConcurrentDictionary<string, bool> visitedUrls = new();

    // Hash set to track duplicate content
    private readonly ConcurrentDictionary<string, bool> contentHashes = new();

    // Scrape the website asynchronously and recursively
    public async Task<ScrapedData?> ScrapeWebsiteAsync(string url, int depth = 1, int maxDepth = 2)
    {
        if (depth > maxDepth || visitedUrls.ContainsKey(url)) return null;

        // Mark the URL as visited
        visitedUrls[url] = true;

        var scrapedData = new ScrapedData { Url = url };

        // Configure AngleSharp with JavaScript support
        var config = Configuration.Default.WithDefaultLoader().WithJs();
        var context = BrowsingContext.New(config);

        // Load the webpage
        var document = await context.OpenAsync(url);

        // Get the displayed text content
        string? textContent = document.Body?.TextContent.Trim();

        // Generate a hash of the content to check for duplicates
        if (!string.IsNullOrEmpty(textContent))
        {
            string contentHash = ComputeHash(textContent);
            if (contentHashes.ContainsKey(contentHash))
            {
                return null; // Skip duplicate content
            }
            contentHashes[contentHash] = true;
            scrapedData.TextContent = textContent;

            // Stub: Add LLM-generated keywords (replace with actual LLM integration)
            scrapedData.Keywords = GenerateKeywordsStub(textContent);
        }

        var uri = new Uri(url);
        // Find all links on the page
        var links = document.QuerySelectorAll("a[href]");

        var tasks = links.Select(async link =>
        {
            string? href = link.GetAttribute("href");
            if (Uri.TryCreate(uri, href, out Uri? absoluteUri))
            {
                try
                {
                    // Recursively scrape linked pages in parallel
                    return await ScrapeWebsiteAsync(absoluteUri.ToString(), depth + 1, maxDepth);
                }
                catch (Exception)
                {
                    // Ignore errors from child pages
                    return null;
                }
            }
            return null;
        });

        var results = await Task.WhenAll(tasks);
        scrapedData.Links.AddRange(results.Where(r => r != null)!);

        //foreach (var link in links)
        //{
        //    string? href = link.GetAttribute("href");
        //    if (Uri.TryCreate(uri, href, out Uri? absoluteUri))
        //    {
        //        try
        //        {
        //            // Recursively scrape linked pages
        //            var childData = await ScrapeWebsiteAsync(absoluteUri.ToString(), depth + 1, maxDepth);
        //            if (childData != null)
        //            {
        //                scrapedData.Links.Add(childData);
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            // Ignore errors from child pages
        //        }
        //    }
        //}

        return scrapedData;
    }

    // Method to compute a hash of the content
    private static string ComputeHash(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    // Stub method to generate up to 10 keywords using LLM (replace with actual LLM integration)
    private static List<string> GenerateKeywordsStub(string content)
    {
        // For now, return the first 10 words as a placeholder for LLM-generated keywords
        return [.. content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      .Take(10)
                      .Select(word => word.Trim('"', '.', ',', ';', ':', '?', '!'))
                      .Distinct()];
    }
}
