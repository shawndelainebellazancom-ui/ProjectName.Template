using Mono.Cecil;
using System.Globalization;
using System.Text;

namespace ProjectName.McpServer.Domain;

public class InspectorService
{
    public string InspectAssembly(byte[] assemblyBytes)
    {
        using var ms = new MemoryStream(assemblyBytes);
        using var assembly = AssemblyDefinition.ReadAssembly(ms);

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"ASSEMBLY: {assembly.Name.Name}");

        foreach (var module in assembly.Modules)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  MODULE: {module.Name}");
            foreach (var type in module.Types)
            {
                // CA1866: Use char overload for single character check
                if (type.Name.StartsWith('<')) continue;

                sb.AppendLine(CultureInfo.InvariantCulture, $"    TYPE: {type.FullName}");
                foreach (var method in type.Methods)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"      METHOD: {method.Name} -> {method.ReturnType.Name}");
                    if (method.HasBody)
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"        [IL Size: {method.Body.Instructions.Count} ops]");
                    }
                }
            }
        }
        return sb.ToString();
    }
}
