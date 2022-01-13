public record BuildData(
    string Version,
    string Configuration,
    DotNetMSBuildSettings MSBuildSettings,
    ClickOnceData ClickOnceData,
    DirectoryPath SolutionDirectory,
    DirectoryPath ArtifactsDirectory,
    DirectoryPath PublishDirectory,
    StorageAccount StorageAccount
    )
{
    public bool ShouldPublish { get; } = !string.IsNullOrEmpty(StorageAccount.Name)
                                            && !string.IsNullOrEmpty(StorageAccount.Container)
                                            && !string.IsNullOrEmpty(StorageAccount.Key);
}