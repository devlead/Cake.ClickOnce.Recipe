/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    bool IsMainBranch,
    DirectoryPath ProjectRoot,
    DotNetMSBuildSettings MSBuildSettings,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath,
    DirectoryPath IntegrationTestsPath
    )
{
    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");
    public DirectoryPath BinaryOutputPath { get; } = OutputPath.Combine("bin");
    public DirectoryPath IntegrationTestsToolPath { get; } = IntegrationTestsPath.Combine("tools");
    public FilePath IntegrationTestsCakePath { get; } = IntegrationTestsPath.CombineWithFilePath("build.cake");

    public string GitHubNuGetSource { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_SOURCE");
    public string GitHubNuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_APIKEY");

    public bool ShouldPushGitHubPackages() => !string.IsNullOrWhiteSpace(GitHubNuGetSource)
                                                && !string.IsNullOrWhiteSpace(GitHubNuGetApiKey);

    public string NuGetSource { get; } = System.Environment.GetEnvironmentVariable("NUGET_SOURCE");
    public string NuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
    public bool ShouldPushNuGetPackages() =>    IsMainBranch &&
                                                !string.IsNullOrWhiteSpace(NuGetSource) &&
                                                !string.IsNullOrWhiteSpace(NuGetApiKey);

    public ICollection<DirectoryPath> DirectoryPathsToClean = new []{
        ArtifactsPath,
        OutputPath,
        IntegrationTestsPath.Combine("tools")
    };


}

private record ExtensionHelper(Func<string, CakeTaskBuilder> TaskCreate, Func<CakeReport> Run);
