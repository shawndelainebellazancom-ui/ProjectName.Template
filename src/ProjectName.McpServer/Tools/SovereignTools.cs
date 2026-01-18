using Microsoft.Playwright;
using ModelContextProtocol.Server;
using ProjectName.McpServer.Domain;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ProjectName.McpServer.Tools;

[McpServerToolType]
public class SovereignTools(
    CompilerService compiler,
    InspectorService inspector,
    BrowserService browser)
{
    // --- ROSLYN TOOLS (THE MAKER) ---

    [McpServerTool(Name = "build_code")]
    [Description("Compiles C# code and checks for errors.")]
    public Task<string> BuildCode(
        [Description("C# Source Code")] string code)
    {
        var result = compiler.Compile(code);
        return Task.FromResult(result.Success
            ? "Build Success. Assembly cached."
            : $"Build Failed:\n{result.Message}");
    }

    // --- CECIL TOOLS (THE INSPECTOR) ---

    [McpServerTool(Name = "inspect_last_build")]
    [Description("Introspects the structure of the last successfully built assembly.")]
    public Task<string> InspectLastBuild()
    {
        if (compiler.LastAssemblyBytes == null) return Task.FromResult("No assembly found. Build code first.");

        var report = inspector.InspectAssembly(compiler.LastAssemblyBytes);
        return Task.FromResult(report);
    }

    // --- PLAYWRIGHT TOOLS (THE EYES) ---

    [McpServerTool(Name = "browse_web")]
    [Description("Navigates to a URL and returns the text content. Useful for reading documentation or scraping data.")]
    public async Task<string> Browse(
        [Description("Target URL")] string url)
    {
        return await browser.ScrapeContentAsync(url);
    }

    [McpServerTool(Name = "take_snapshot")]
    [Description("Captures a visual screenshot of the target URL (Base64). Use this when scraping fails to see what is happening on the screen (e.g., loading spinners, errors).")]
    public async Task<string> TakeSnapshot(
        [Description("Target URL")] string url)
    {
        var base64 = await browser.TakeSnapshotAsync(url);
        return $"Snapshot captured for {url}. (Base64 Length: {base64.Length})";
    }
}
