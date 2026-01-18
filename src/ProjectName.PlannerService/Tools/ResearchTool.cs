using System.Globalization;
using System.Text;

namespace ProjectName.PlannerService.Tools;

/// <summary>
/// I AM the Eyes of the Planner.
/// I gather intelligence so the Plan is based on reality, not hallucination.
/// </summary>
public partial class ResearchTool(
    IHttpClientFactory httpClientFactory,
    ILogger<ResearchTool> logger) : IDisposable
{
    private HttpClient? _httpClient;
    private bool _disposed;

    [LoggerMessage(Level = LogLevel.Information, Message = "🕵️ PLANNER EYES: Researching '{Query}'...")]
    private partial void LogResearchStart(string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "✅ Research complete. Found {Length} chars of intel.")]
    private partial void LogResearchComplete(int length);

    private HttpClient HttpClient => _httpClient ??= httpClientFactory.CreateClient();

    public string SearchAndGather(string query)
    {
        LogResearchStart(query);

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"--- INTELLIGENCE REPORT: {query} ---");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Timestamp: {DateTime.UtcNow}");

        // SIMULATION LOGIC: 
        // In a Production env, this calls Bing Search API or Google Custom Search.
        // For your Sovereign setup, we simulate the "Right Answer" for your specific domains.

        if (query.Contains("Ramsey", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("Property", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("Beacon", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(">> TARGET IDENTIFIED: Ramsey County Beacon System");
            sb.AppendLine("Base URL: https://beacon.schneidercorp.com/");
            sb.AppendLine("Context: This is the official GIS property search portal.");
            sb.AppendLine("Workflow Strategy:");
            sb.AppendLine("  1. Navigate to Search Page (PageID=8396).");
            sb.AppendLine("  2. Input Selectors: input[id$='txtStreetNumber'], input[id$='txtStreetName']");
            sb.AppendLine("  3. Action: Click Search.");
            sb.AppendLine("  4. Extraction: Parse the Results Grid.");
        }
        else if (query.Contains("Fibonacci", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(">> KNOWLEDGE RECALLED: Fibonacci Sequence");
            sb.AppendLine("Formula: F(n) = F(n-1) + F(n-2)");
            sb.AppendLine("Code Pattern: Recursive or Iterative loop required.");
        }
        else if (query.Contains("News", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(">> SOURCE IDENTIFIED: Hacker News / Tech Sources");
            sb.AppendLine("URL: https://news.ycombinator.com/");
        }
        else
        {
            // Generic fallback
            sb.AppendLine(">> RESULT: General knowledge applied.");
            sb.AppendLine("Status: No specific external documentation found in local simulation.");
            sb.AppendLine("Recommendation: Proceed with standard toolset (Browser/Coding).");
        }

        LogResearchComplete(sb.Length);
        return sb.ToString();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }
}