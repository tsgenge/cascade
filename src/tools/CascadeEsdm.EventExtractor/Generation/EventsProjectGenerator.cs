namespace CascadeEsdm.EventExtractor.Generation;

/// <summary>
/// Generates the .csproj file for the events assembly.
/// The project is only written if it does not already exist, or if overwrite is enabled.
/// Version management is intentionally left to the author.
/// </summary>
public static class EventsProjectGenerator
{
    public static void Generate(
        string outputDir,
        string assemblyName,
        string rootNamespace,
        bool overwrite)
    {
        var projectFileName = assemblyName + ".csproj";
        var projectFilePath = Path.Combine(outputDir, projectFileName);

        if (File.Exists(projectFilePath) && !overwrite)
            return;

        Directory.CreateDirectory(outputDir);

        var content = BuildProjectXml(assemblyName, rootNamespace);
        File.WriteAllText(projectFilePath, content);
    }

    private static string BuildProjectXml(string assemblyName, string rootNamespace) => $"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <AssemblyName>{assemblyName}</AssemblyName>
            <RootNamespace>{rootNamespace}</RootNamespace>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <LangVersion>latest</LangVersion>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="CascadeEsdm.SharedKernel.Abstractions" Version="*" />
          </ItemGroup>

        </Project>
        """;
}
