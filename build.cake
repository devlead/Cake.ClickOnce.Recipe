// Install modules
#module "nuget:?package=Cake.DotNetTool.Module&version=0.4.0"

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

        context.Information("Building version {0}", version);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        return new BuildData(
            version,
            "src",
            new DotNetCoreMSBuildSettings()
                .SetConfiguration("Release")
                .SetVersion(version)
                .WithProperty("Copyright", $"Mattias Karlsson © {DateTime.UtcNow.Year}")
                .WithProperty("Authors", "devlead")
                .WithProperty("Company", "devlead")
                .WithProperty("PackageLicenseExpression", "MIT")
                .WithProperty("PackageTags", "Cake;Build;Recipe;ClickOnce")
                .WithProperty("PackageDescription", "Opinionated Cake recipe for simplifying the publishing of .NET 5 Windows application using GitHub actions, Cake and ClickOnce to Azure Blob Storage.")
                .WithProperty("PackageIconUrl", "https://cdn.jsdelivr.net/gh/cake-contrib/graphics@a5cf0f881c390650144b2243ae551d5b9f836196/png/cake-contrib-medium.png")
                .WithProperty("PackageIcon", "cake-contrib-medium.png")
                .WithProperty("PackageProjectUrl", "https://github.com/devlead/Cake.ClickOnce.Recipe")
                .WithProperty("RepositoryUrl", "https://github.com/devlead/Cake.ClickOnce.Recipe.git")
                .WithProperty("RepositoryType", "git")
                .WithProperty("ContinuousIntegrationBuild", gh.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version)
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
        static (context, data) => context.DotNetCoreRestore(
            data.ProjectRoot.FullPath,
            new DotNetCoreRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
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
    .Default()
    .Does<BuildData>(
        static (context, data) => context.DotNetCorePack(
            data.ProjectRoot.FullPath,
            new DotNetCorePackSettings {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Run();