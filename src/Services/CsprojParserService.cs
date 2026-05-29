using System.IO;
using System.Linq;
using System.Xml.Linq;
using GeneralUpdate.Tools.Models;

namespace GeneralUpdate.Tools.Services;

public static class CsprojParserService
{
    public static CsprojInfo? Parse(string csprojPath)
    {
        if (string.IsNullOrWhiteSpace(csprojPath) || !File.Exists(csprojPath))
            return null;

        var doc = XDocument.Load(csprojPath);
        var projectDir = Path.GetDirectoryName(Path.GetFullPath(csprojPath)) ?? "";
        var projectName = Path.GetFileNameWithoutExtension(csprojPath);

        var assemblyName = GetElementValue(doc, "AssemblyName")
                        ?? projectName;

        var outputType = GetElementValue(doc, "OutputType") ?? "WinExe";

        var targetFramework = GetElementValue(doc, "TargetFramework")
                           ?? GetElementValue(doc, "TargetFrameworks");

        return new CsprojInfo
        {
            ProjectName = projectName,
            AssemblyName = assemblyName.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)
                ? assemblyName
                : assemblyName + ".exe",
            OutputType = outputType,
            TargetFramework = targetFramework ?? "",
            CsprojPath = Path.GetFullPath(csprojPath),
            ProjectDir = projectDir
        };
    }

    private static string? GetElementValue(XContainer doc, string elementName)
    {
        var ns = doc.Document?.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var el = doc.Descendants(ns + elementName).FirstOrDefault();
        if (el != null && !string.IsNullOrWhiteSpace(el.Value))
            return el.Value.Trim();

        // Fallback: search without namespace
        foreach (var d in doc.Descendants())
        {
            if (d.Name.LocalName == elementName && !string.IsNullOrWhiteSpace(d.Value))
                return d.Value.Trim();
        }

        return null;
    }
}
