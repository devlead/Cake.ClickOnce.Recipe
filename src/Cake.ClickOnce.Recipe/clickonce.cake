public record ClickOnceData(
    string AppName,
    string Publisher,
    string PublishUrl,
    string TargetDirectoryName,
    string Version
    )
{
    private static string GetApplicationManifestFile(string appName)
        => $"{appName}.manifest";
    private static string GetTargetApplicationManifestFile(string targetDirectoryName, string appName)
        => $"{targetDirectoryName}/{GetApplicationManifestFile(appName)}";

    public FilePath DeploymentManifestFile { get; } = $"{AppName}.application";
    public FilePath AppExeFile { get; } = $"{AppName}.exe";
    public FilePath ApplicationManifestFile { get; } = GetApplicationManifestFile(AppName);
    public FilePath TargetApplicationManifestFile { get; } = GetTargetApplicationManifestFile(TargetDirectoryName, AppName);
    public string AppCodeBase { get; } = $"{PublishUrl}/{GetTargetApplicationManifestFile(TargetDirectoryName, AppName)}";

}

public static void MageTool(
    this ICakeContext context,
    DirectoryPath workingDirectory,
    Func<ProcessArgumentBuilder, ProcessArgumentBuilder> args
    )
{
    var localTool = context.Tools.Resolve("dotnet-mage.exe");
    var arguments = new ProcessArgumentBuilder();
    if (localTool == null)
    {
        arguments.Append("mage");
        localTool = context.Tools.Resolve("dotnet.exe") ?? throw new Exception("Failed to resolve dotnet exe.");
    }

    var resultCode = context.StartProcess(
            localTool,
            new ProcessSettings {
                Arguments = args(arguments),
                WorkingDirectory =  workingDirectory
            }
        );

    if (resultCode != 0)
    {
        throw new Exception($"Failed to execute {localTool} ({resultCode})");
    }
}

public static void MageToolAddLauncher(
    this ICakeContext context,
    DirectoryPath workingDirectory,
    ClickOnceData clickOnceData
)
    => context.MageTool(
                    workingDirectory,
                    args => args
                            .AppendSwitchQuoted(
                                "-AddLauncher",
                                " ",
                                clickOnceData.AppExeFile.FullPath
                            )
                            .AppendSwitchQuoted(
                                "-TargetDirectory",
                                " ",
                                clickOnceData.TargetDirectoryName
                            )
                );

public static void MageToolNewApplication(
    this ICakeContext context,
    DirectoryPath workingDirectory,
    ClickOnceData clickOnceData
)
    => context.MageTool(
                    workingDirectory,
                    args => args
                            .AppendSwitchQuoted(
                                "-New",
                                " ",
                                "Application"
                            )
                            .AppendSwitchQuoted(
                                "-ToFile",
                                " ",
                                clickOnceData.TargetApplicationManifestFile.FullPath
                            )
                            .AppendSwitchQuoted(
                                "-FromDirectory",
                                " ",
                                clickOnceData.TargetDirectoryName
                            )
                            .AppendSwitchQuoted(
                                "-Version",
                                " ",
                                clickOnceData.Version
                            )
                );

public static void MageToolNewDeployment(
    this ICakeContext context,
    DirectoryPath workingDirectory,
    ClickOnceData clickOnceData
)
    => context.MageTool(
                    workingDirectory,
                    args => args
                                .AppendSwitchQuoted(
                                    "-New",
                                    " ",
                                    "Deployment"
                                )
                                .AppendSwitchQuoted(
                                    "-Install",
                                    " ",
                                    "true"
                                )
                                    .AppendSwitchQuoted(
                                    "-AppManifest",
                                    " ",
                                    clickOnceData.TargetApplicationManifestFile.FullPath
                                )
                                .AppendSwitchQuoted(
                                    "-AppCodeBase",
                                    " ",
                                    clickOnceData.AppCodeBase
                                )
                                .AppendSwitchQuoted(
                                    "-Publisher",
                                    " ",
                                    clickOnceData.Publisher
                                )
                                .AppendSwitchQuoted(
                                    "-Version",
                                    " ",
                                    clickOnceData.Version
                                )
                                .AppendSwitchQuoted(
                                    "-ToFile",
                                    " ",
                                    clickOnceData.DeploymentManifestFile.FullPath
                                )
    );

public static void MageToolUpdateDeploymentMinVersion(
    this ICakeContext context,
    DirectoryPath workingDirectory,
    ClickOnceData clickOnceData
) => context.MageTool(
                    workingDirectory,
                    args => args
                                .AppendSwitchQuoted(
                                    "-Update",
                                    " ",
                                    clickOnceData.DeploymentManifestFile.FullPath
                                )
                                .AppendSwitchQuoted(
                                    "-MinVersion",
                                    " ",
                                    clickOnceData.Version
                                )
            );


public static void CreateAppRef(
    this ICakeContext context,
    ClickOnceData clickOnceData,
    DirectoryPath targetDirectory
)
{
    var appRefFilePath = targetDirectory.CombineWithFilePath($"{clickOnceData.AppName}.appref-ms");
    using(var stream = context.FileSystem.GetFile(appRefFilePath).OpenWrite())
    using(System.IO.StreamWriter streamWriter = new(stream, System.Text.Encoding.Unicode))
    {
        streamWriter.WriteLine(
            context
                .TransformText("<%publishUrl%>/<%deploymentManifest%>#<%appName%>.app, Culture=neutral, PublicKeyToken=0000000000000000, processorArchitecture=msil")
                .WithToken("publishUrl", clickOnceData.PublishUrl.TrimEnd('/'))
                .WithToken("deploymentManifest", clickOnceData.DeploymentManifestFile.FullPath)
                .WithToken("appName", clickOnceData.AppName)
        );
    }
}
