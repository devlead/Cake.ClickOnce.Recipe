#load "blobupload.cake"
#load "clickonce.cake"
#load "builddata.cake"
#load "version.cake"

public partial class ClickOnce
{
    public static string ApplicationName { get; set; }
    public static string Publisher { get; set; }
    public static string PublishUrl { get; set; }
    public static string Configuration { get; set; } = "Release";
    public static DirectoryPath SolutionDirectory { get; set; } = "./src";
    public static DirectoryPath ArtifactsDirectory { get; set; } = "./artifacts";
    public static string Version { get; set; } = GetDateBasedVersion();
    private static string GetDateBasedVersion()
    {
        var buildDate = DateTime.UtcNow;
        var timeOfDay = (short)((buildDate - buildDate.Date).TotalSeconds/3);
        var version = $"{buildDate:yyyy}.{buildDate:MM}.{buildDate:dd}.{timeOfDay}";
        return version;
    }
    public static Action RunBuild { get; set;}
}



ClickOnce.RunBuild = () => {
    Information("Cake.ClickOnce.Recipe {0}", ClickOnce.Recipe.Version);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.ApplicationName)}: {{0}}", ClickOnce.ApplicationName);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.Publisher)}: {{0}}", ClickOnce.Publisher);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.PublishUrl)}: {{0}}", ClickOnce.PublishUrl);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.Configuration)}: {{0}}", ClickOnce.Configuration);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.Version)}: {{0}}", ClickOnce.Version);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.SolutionDirectory)}: {{0}}", ClickOnce.SolutionDirectory);
    Information($"{nameof(ClickOnce)}.{nameof(ClickOnce.ArtifactsDirectory)}: {{0}}", ClickOnce.ArtifactsDirectory);

    if (string.IsNullOrEmpty(ClickOnce.ApplicationName))
    {
        throw new Exception($"{nameof(ClickOnce)}.{nameof(ClickOnce.ApplicationName)} not specified.");
    }

    if (string.IsNullOrEmpty(ClickOnce.Publisher))
    {
        throw new Exception($"{nameof(ClickOnce)}.{nameof(ClickOnce.Publisher)} not specified.");
    }

    if (string.IsNullOrEmpty(ClickOnce.PublishUrl))
    {
        throw new Exception($"{nameof(ClickOnce)}.{nameof(ClickOnce.PublishUrl)} not specified.");
    }

    RunTarget(
        Argument(
            "target",
            (
                BuildSystem.GitHubActions.Environment.PullRequest.IsPullRequest
                || !BuildSystem.GitHubActions.IsRunningOnGitHubActions
            )   ? "Default"
                : "Publish-ClickOnce"
            )
    );
};

if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
{
    TaskSetup(context => System.Console.WriteLine($"::group::{context.Task.Name.Quote()}"));
    TaskTeardown(context => System.Console.WriteLine("::endgroup::"));
}

Setup(context=>{

    context.Information("Setting up version {0}", ClickOnce.Version);

    var artifactsDirectory = context.MakeAbsolute(ClickOnce.ArtifactsDirectory);

    var publishDirectory = artifactsDirectory.Combine($"{ClickOnce.ApplicationName}.{ClickOnce.Version}");

    return new BuildData(
        ClickOnce.Version,
        ClickOnce.Configuration,
        new DotNetCoreMSBuildSettings()
                                .WithProperty("Version", ClickOnce.Version)
                                .WithProperty("Configuration", ClickOnce.Configuration),
        new ClickOnceData(
            ClickOnce.ApplicationName,
            ClickOnce.Publisher,
            ClickOnce.PublishUrl,
            publishDirectory.GetDirectoryName(),
            ClickOnce.Version
        ),
        context.MakeAbsolute(ClickOnce.SolutionDirectory),
        artifactsDirectory,
        publishDirectory,
        new StorageAccount(
            context.EnvironmentVariable("PUBLISH_STORAGE_ACCOUNT"),
            context.EnvironmentVariable("PUBLISH_STORAGE_CONTAINER"),
            context.EnvironmentVariable("PUBLISH_STORAGE_KEY")
        )
    );
});

Task("Clean")
    .Does<BuildData>(
        (context, data) => {
            context.CleanDirectories(
                new []{
                    data.ArtifactsDirectory,
                    data.PublishDirectory
                });
            context.CleanDirectories("./src/**/bin/" + data.Configuration);
            context.CleanDirectories("./src/**/obj");
        }
    );

Task("Restore")
    .IsDependentOn("Clean")
    .Does<BuildData>(
        (context, data)=> {
            context.DotNetCoreRestore(
                data.SolutionDirectory.FullPath,
                new DotNetCoreRestoreSettings {
                    MSBuildSettings = data.MSBuildSettings
                }
            );
        }
    );

Task("Build")
    .IsDependentOn("Restore")
    .Does<BuildData>(
        (context, data)=> {
            context.DotNetCoreBuild(
                data.SolutionDirectory.FullPath,
                new DotNetCoreBuildSettings {
                    NoRestore = true,
                    MSBuildSettings = data.MSBuildSettings
                }
            );
        }
    );

Task("Publish")
    .IsDependentOn("Build")
    .Does<BuildData>(
        (context, data)=> {
            context.DotNetCorePublish(
                data.SolutionDirectory.FullPath,
                new DotNetCorePublishSettings {
                    NoRestore = true,
                    NoBuild = true,
                    OutputDirectory = data.PublishDirectory,
                    MSBuildSettings = data.MSBuildSettings
                }
            );
        }
    );

Task("ClickOnce-Launcher")
    .IsDependentOn("Publish")
    .Does<BuildData>(
        (context, data)=> {
            context.MageToolAddLauncher(
                    data.ArtifactsDirectory,
                   data.ClickOnceData
                );
        }
    );

Task("ClickOnce-Application-Manifest")
    .IsDependentOn("ClickOnce-Launcher")
    .Does<BuildData>(
        (context, data)=> {
            context.MageToolNewApplication(
                    data.ArtifactsDirectory,
                    data.ClickOnceData
                );
        }
    );

Task("ClickOnce-Deployment-Manifest")
    .IsDependentOn("ClickOnce-Application-Manifest")
    .Does<BuildData>(
        (context, data)=> {
            context.MageToolNewDeployment(
                    data.ArtifactsDirectory,
                    data.ClickOnceData
            );
        }
    );

Task("ClickOnce-Deployment-UpdateManifest")
    .IsDependentOn("ClickOnce-Deployment-Manifest")
    .Does<BuildData>(
        (context, data)=> {
            context.MageToolUpdateDeploymentMinVersion(
                    data.ArtifactsDirectory,
                    data.ClickOnceData
            );
        }
    );

Task("ClickOnce-Deployment-CreateAppRef")
    .IsDependentOn("ClickOnce-Deployment-UpdateManifest")
    .Does<BuildData>(
        (context, data)=> {
            context.CreateAppRef(
                    data.ClickOnceData,
                    data.ArtifactsDirectory
            );
        }
    );

Task("ClickOnce-Upload-Version")
    .IsDependentOn("ClickOnce-Deployment-CreateAppRef")
    .WithCriteria<BuildData>((context, data) => data.ShouldPublish)
    .Does<BuildData>(
        async (context, data)=> {
            await System.Threading.Tasks.Task.WhenAll(
                context
                    .GetFiles($"{data.PublishDirectory}/**/*.*")
                    .Select(file=> context.UploadToBlobStorage(
                    data.StorageAccount,
                    file,
                    data.ArtifactsDirectory.GetRelativePath(file)
                    ))
            );
        }
    );

Task("ClickOnce-Upload-Application")
    .IsDependentOn("ClickOnce-Upload-Version")
    .WithCriteria<BuildData>((context, data) => data.ShouldPublish)
    .Does<BuildData>(
        async (context, data)=> {
           await System.Threading.Tasks.Task.WhenAll(
                context
                    .GetFiles($"{data.ArtifactsDirectory}/*.*")
                    .Select(file=> context.UploadToBlobStorage(
                    data.StorageAccount,
                    file,
                    data.ArtifactsDirectory.GetRelativePath(file)
                    ))
            );
        }
    );

Task("Default")
    .IsDependentOn("ClickOnce-Deployment-Manifest");

Task("Publish-ClickOnce")
    .IsDependentOn("ClickOnce-Upload-Application");