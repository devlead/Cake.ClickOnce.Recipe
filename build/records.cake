/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
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

    public ICollection<DirectoryPath> DirectoryPathsToClean = new []{
        ArtifactsPath,
        OutputPath,
        IntegrationTestsPath.Combine("tools")
    };


}

private record ExtensionHelper(Func<string, CakeTaskBuilder> TaskCreate, Func<CakeReport> Run);
