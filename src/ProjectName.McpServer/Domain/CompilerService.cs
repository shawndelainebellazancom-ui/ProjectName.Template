using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using System.Text;

namespace ProjectName.McpServer.Domain;

public partial class CompilerService(ILogger<CompilerService> logger)
{
    public byte[]? LastAssemblyBytes { get; private set; }
    public List<string> LastDiagnostics { get; private set; } = [];

    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "Compiling source code...")]
    private partial void LogCompiling();

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Compilation Success.")]
    private partial void LogSuccess();

    [LoggerMessage(EventId = 102, Level = LogLevel.Warning, Message = "Compilation Failed.")]
    private partial void LogFailure();

    public CompilationResult Compile(string sourceCode)
    {
        LogCompiling();

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // IDE0305 FIX: Use Collection Expression [...]
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        ];

        var compilation = CSharpCompilation.Create(
            string.Format(CultureInfo.InvariantCulture, "Dynamic_{0:N}", Guid.NewGuid()),
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        LastDiagnostics = result.Diagnostics
            .Select(d => string.Format(CultureInfo.InvariantCulture, "[{0}] {1}: {2}", d.Severity, d.Id, d.GetMessage(CultureInfo.InvariantCulture)))
            .ToList();

        if (result.Success)
        {
            ms.Seek(0, SeekOrigin.Begin);
            LastAssemblyBytes = ms.ToArray();
            LogSuccess();
            return new CompilationResult(true, "Compilation Successful", LastAssemblyBytes);
        }
        else
        {
            LogFailure();
            return new CompilationResult(false, string.Join("\n", LastDiagnostics), null);
        }
    }
}

public record CompilationResult(bool Success, string Message, byte[]? AssemblyBytes);
