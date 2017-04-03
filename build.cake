#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console&version=2.2.0"

#addin "Cake.FileHelpers"

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var artifactsDir    = Directory("./artifacts");
var solution        = "./src/AspNetCoreHttpMessageHandler.sln";
GitVersion versionInfo = null;



Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
});

Task("SetVersionInfo")
    .IsDependentOn("Clean")
    .Does(() =>
{
    versionInfo = GitVersion(new GitVersionSettings {
        RepositoryPath = "."
    });
});

Task("RestorePackages")
    .IsDependentOn("SetVersionInfo")
    .Does(() =>
{
    NuGetRestore(solution);
    DotNetCoreRestore(solution);
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var buildSettings = new MSBuildSettings {
        Configuration = configuration,
        ToolVersion = MSBuildToolVersion.VS2017,
        Verbosity = Verbosity.Minimal,
        ArgumentCustomization = args => args.Append("/p:SemVer=" + versionInfo.NuGetVersionV2)
    };
    MSBuild(solution, buildSettings);
});


Task("RunTests")
    .IsDependentOn("Build")
    .Does(() =>
{
        var settings =  new DotNetCoreTestSettings 
        { 
            NoBuild = true
        };
        DotNetCoreTest("./src/AspNetCoreHttpMessageHandler.Tests/AspNetCoreHttpMessageHandler.Tests.csproj", settings);
});

Task("NuGetPack")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Version " + versionInfo.NuGetVersionV2);
    var settings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        OutputDirectory = "./artifacts/",
        NoBuild = true
    };
    DotNetCorePack("./src/AspNetCoreHttpMessageHandler", settings);
});

Task("NuGetPublish")
    .IsDependentOn("NuGetPack")
    .Does(() =>
    {
        var APIKey = EnvironmentVariable("NUGETAPIKEY");

        var packages = GetFiles("./artifacts/*.nupkg");
        NuGetPush(packages, new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = APIKey
        }
    });

    })

Task("Default")
    .IsDependentOn("RunTests")
    .IsDependentOn("NuGetPublish");

RunTarget(target);