// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Aspire.Hosting.Tests;

public class MSBuildTests
{
    /// <summary>
    /// Tests that when an AppHost has a ProjectReference to a library project, a warning is emitted.
    /// </summary>
    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/8467")]
    public void EnsureWarningsAreEmittedWhenProjectReferencingLibraries()
    {
        var repoRoot = MSBuildUtils.GetRepoRoot();
        using var tempDirectory = new TempDirectory();

        var libraryDirectory = Path.Combine(tempDirectory.Path, "Library");
        Directory.CreateDirectory(libraryDirectory);

        File.WriteAllText(Path.Combine(libraryDirectory, "Library.csproj"), """
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
""");
        File.WriteAllText(Path.Combine(libraryDirectory, "Class1.cs"), """
namespace Library;

public class Class1
{
}
""");

        var appHostDirectory = Path.Combine(tempDirectory.Path, "AppHost");
        Directory.CreateDirectory(appHostDirectory);

        File.WriteAllText(Path.Combine(appHostDirectory, "AppHost.csproj"), $"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireHost>true</IsAspireHost>

    <!-- 
      Test applications have their own way of referencing Aspire.Hosting.AppHost, as well as DCP and Dashboard, so we disable
      the Aspire.AppHost.SDK targets that will automatically add these references to projects. 
    -->
    <SkipAddAspireDefaultReferences Condition="'$(TestsRunningOutsideOfRepo)' != 'true'">true</SkipAddAspireDefaultReferences>
    <AspireHostingSDKVersion>9.0.0</AspireHostingSDKVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="{repoRoot}\src\Aspire.Hosting.AppHost\Aspire.Hosting.AppHost.csproj" IsAspireProjectResource="false" />

    <ProjectReference Include="..\Library\Library.csproj" />
  </ItemGroup>

</Project>
""");
        File.WriteAllText(Path.Combine(appHostDirectory, "AppHost.cs"), """
var builder = DistributedApplication.CreateBuilder();
builder.Build().Run();
""");

        File.WriteAllText(Path.Combine(appHostDirectory, "Directory.Build.props"), $"""
<Project>
  <PropertyGroup>
    <SkipAspireWorkloadManifest>true</SkipAspireWorkloadManifest>
  </PropertyGroup>

  <Import Project="{repoRoot}\src\Aspire.Hosting.AppHost\build\Aspire.Hosting.AppHost.props" />
</Project>
""");
        File.WriteAllText(Path.Combine(appHostDirectory, "Directory.Build.targets"), $"""
<Project>
  <Import Project="{repoRoot}\src\Aspire.Hosting.AppHost\build\Aspire.Hosting.AppHost.in.targets" />
  <Import Project="{repoRoot}\src\Aspire.AppHost.Sdk\SDK\Sdk.in.targets" />
</Project>
""");

        var output = new StringBuilder();
        var outputDone = new ManualResetEvent(false);
        using var process = new Process();
        // set '-nodereuse:false -p:UseSharedCompilation=false' so the MSBuild and Roslyn server processes don't hang around, which may hang the test in CI
        process.StartInfo = new ProcessStartInfo("dotnet", $"build -nodereuse:false -p:UseSharedCompilation=false")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = appHostDirectory
        };
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                outputDone.Set();
            }
            else
            {
                output.AppendLine(e.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();

        Assert.True(process.WaitForExit(milliseconds: 180_000), "dotnet build command timed out after 3 minutes.");
        Assert.True(process.ExitCode == 0, $"Build failed: {Environment.NewLine}{output}");

        Assert.True(outputDone.WaitOne(millisecondsTimeout: 60_000), "Timed out waiting for output to complete.");

        // Ensure a warning is emitted when an AppHost references a Library project
        Assert.Contains("warning ASPIRE004", output.ToString());
    }
}
