// Install .NET Core Global tools.
#tool "dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=5.6.4"

#load "build/records.cake"
#load "build/helpers.cake"

/*****************************
 * Setup
 *****************************/
Setup(
    static context => {

        var assertedVersions = context.GitVersion(new GitVersionSettings
            {
                OutputType = GitVersionOutput.Json
            });

        var gh = context.GitHubActions();
        var version = assertedVersions.LegacySemVerPadded;
        var branchName = assertedVersions.BranchName;
        var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("main", branchName);

        context.Information("Building version {0} (Branch: {1}, IsMain: {2})",
            version,
            branchName,
            isMainBranch);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        var integrationTestsPath = context
                            .MakeAbsolute(context.Directory("./tests/integration"));

        return new BuildData(
            version,
            isMainBranch,
            "src",
            new DotNetMSBuildSettings()
                .SetConfiguration("Release")
                .SetVersion(version)
                .WithProperty("Copyright", $"Mattias Karlsson © {DateTime.UtcNow.Year}")
                .WithProperty("Authors", "devlead")
                .WithProperty("Company", "devlead")
                .WithProperty("PackageLicenseExpression", "MIT")
                .WithProperty("PackageTags", "Cake;Build;Recipe;ClickOnce;cake-recipe")
                .WithProperty("PackageDescription", "Opinionated Cake recipe for simplifying the publishing of .NET 5 Windows application using GitHub actions, Cake and ClickOnce to Azure Blob Storage.")
                .WithProperty("PackageIconUrl", "https://cdn.jsdelivr.net/gh/cake-contrib/graphics@a5cf0f881c390650144b2243ae551d5b9f836196/png/cake-contrib-medium.png")
                .WithProperty("PackageIcon", "cake-contrib-medium.png")
                .WithProperty("PackageProjectUrl", "https://github.com/devlead/Cake.ClickOnce.Recipe")
                .WithProperty("RepositoryUrl", "https://github.com/devlead/Cake.ClickOnce.Recipe.git")
                .WithProperty("RepositoryType", "git")
                .WithProperty("ContinuousIntegrationBuild", gh.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version),
            integrationTestsPath
            );
    }
);

/*****************************
 * Tasks
 *****************************/
Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    )
.Then("Restore")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            data.ProjectRoot.FullPath,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("DPI")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
                "tool",
                new DotNetToolSettings {
                    EnvironmentVariables = {
                        { "RECIPE_SOURCE",  data.NuGetOutputPath.FullPath },
                        { "RECIPE_VERSION",  data.Version },
                        { "CAKE_PATHS_TOOLS", data.IntegrationTestsToolPath.FullPath },
                        { "CAKE_PATHS_ADDINS", data.IntegrationTestsToolPath.Combine("Addins").FullPath },
                        { "CAKE_PATHS_MODULES", data.IntegrationTestsToolPath.Combine("Modules").FullPath }
                    },
                    ArgumentCustomization = args => args
                                                        .Append("run")
                                                        .Append("dpi")
                                                        .Append("nuget")
                                                        .Append("--silent")
                                                        .AppendSwitchQuoted("--output", "table")
                                                        .Append(
                                                            (
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_SharedKey"))
                                                                &&
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_WorkspaceId"))
                                                            )
                                                                ? "report"
                                                                : "analyze"
                                                            )
                                                        .AppendSwitchQuoted("--buildversion", data.Version)
                }
            )
    )
.Then("Write-Version")
    .Does<BuildData>(
        static (context, data) => {
            System.IO.File.WriteAllText(
                "src/Cake.ClickOnce.Recipe/version.cake",
                $@"public partial class ClickOnce
{{
    public class Recipe
    {{
        public const string Version = ""{(context.BuildSystem().GitHubActions.IsRunningOnGitHubActions ? data.Version : "0.0.0")}"";
    }}
}}"
            );
        }
    )
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetPack(
            data.ProjectRoot.FullPath,
            new DotNetPackSettings {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Integration-Tests")
    .Default()
    .Does<BuildData>(
        static (context, data) => context.CakeExecuteScript(
            data.IntegrationTestsCakePath,
            new CakeSettings {
                EnvironmentVariables = {
                    { "RECIPE_SOURCE",  data.NuGetOutputPath.FullPath },
                    { "RECIPE_VERSION",  data.Version },
                    { "CAKE_PATHS_TOOLS", data.IntegrationTestsToolPath.FullPath },
                    { "CAKE_PATHS_ADDINS", data.IntegrationTestsToolPath.Combine("Addins").FullPath },
                    { "CAKE_PATHS_MODULES", data.IntegrationTestsToolPath.Combine("Modules").FullPath }
                }
            }
        )
    )
.Then("Upload-Artifacts")
    .WithCriteria(BuildSystem.IsRunningOnGitHubActions, nameof(BuildSystem.IsRunningOnGitHubActions))
    .Does<BuildData>(
        static (context, data) => context
            .GitHubActions()
            .Commands
            .UploadArtifact(data.ArtifactsPath, "artifacts")
    )
.Then("Push-GitHub-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushGitHubPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
            new DotNetNuGetPushSettings
            {
                Source = data.GitHubNuGetSource,
                ApiKey = data.GitHubNuGetApiKey
            }
        )
    )
.Then("Push-NuGet-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
                new DotNetNuGetPushSettings
                {
                    Source = data.NuGetSource,
                    ApiKey = data.NuGetApiKey
                }
        )
    )
.Then("GitHub-Actions")
.Run();