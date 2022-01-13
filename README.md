# Cake.ClickOnce.Recipe

Opinionated Cake recipe for simplifying the publishing of .NET 6 Windows application using GitHub actions, Cake and ClickOnce to Azure Blob Storage.

## Usage

### Cake Example

```csharp
#load "nuget:?package=Cake.ClickOnce.Recipe&version=0.6.0"

ClickOnce.ApplicationName = "MyApp";
ClickOnce.Publisher = "devlead";
ClickOnce.PublishUrl = "https://clickoncenet5test.blob.core.windows.net/publish";
ClickOnce.RunBuild();
```

### GitHub yaml Example

```yaml
name: Build
on:
  pull_request:
  push:
    branches:
      - main
      - develop

jobs:
  build:
    name: Build
    runs-on: windows-latest
    steps:
      - name: Get the sources
        uses: actions/checkout@v2

      - name: Install .NET Core SDK
        uses: actions/setup-dotnet@v1

      - name: Run Cake script
        uses: cake-build/cake-action@v1
        env:
          PUBLISH_STORAGE_ACCOUNT: ${{ secrets.PUBLISH_STORAGE_ACCOUNT }}
          PUBLISH_STORAGE_CONTAINER: ${{ secrets.PUBLISH_STORAGE_CONTAINER }}
          PUBLISH_STORAGE_KEY: ${{ secrets.PUBLISH_STORAGE_KEY }}
        with:
          cake-version: tool-manifest
```

### Output Example on GitHub actions

```bash

----------------------------------------
Setup
----------------------------------------
Setting up version 2021.01.27.18139
▶ "Clean"
▶ "Restore"
▶ "Build"
▶ "Publish"
▶ "ClickOnce-Launcher"
▶ "ClickOnce-Application-Manifest"
▶ "ClickOnce-Deployment-Manifest"
▶ "ClickOnce-Deployment-UpdateManifest"
▶ "ClickOnce-Deployment-CreateAppRef"
▶ "ClickOnce-Upload-Version"
▶ "ClickOnce-Upload-Application"
▶ "Publish-ClickOnce"

Task                                Duration
--------------------------------------------------------
Setup                               00:00:00.0160939
Clean                               00:00:00.0084806
Restore                             00:00:02.1274733
Build                               00:00:03.3076849
Publish                             00:00:01.2192429
ClickOnce-Launcher                  00:00:00.4506914
ClickOnce-Application-Manifest      00:00:00.6510728
ClickOnce-Deployment-Manifest       00:00:00.9086913
ClickOnce-Deployment-UpdateManifest 00:00:00.6800874
ClickOnce-Deployment-CreateAppRef   00:00:00.0112772
ClickOnce-Upload-Version            00:00:02.1736495
ClickOnce-Upload-Application        00:00:00.6269294
--------------------------------------------------------
Total:                              00:00:12.1814083
```

## Settings

### Cake

| Setting                      | Type          | Default value          | Example                                                      |
|------------------------------|---------------|------------------------|--------------------------------------------------------------|
| ClickOnce.ApplicationName    | string        | `null`                 | `MyApp`                                                      |
| ClickOnce.Publisher          | string        | `null`                 | `devlead`                                                    |
| ClickOnce.PublishUrl         | string        | `null`                 | `https://{storageaccount}.blob.core.windows.net/{container}` |
| ClickOnce.Configuration      | string        | `Release`              | `Release`                                                    |
| ClickOnce.SolutionDirectory  | DirectoryPath | `./src`                | `./src`                                                      |
| ClickOnce.ArtifactsDirectory | DirectoryPath | `./artifacts`          | `./artifacts`                                                |
| ClickOnce.Version            | string        | `yyyy.MM.dd.timeOfDay` | `2021.01.27.18292`                                           |

### Environment variables

| Name                      | Description                  |
|---------------------------|------------------------------|
| PUBLISH_STORAGE_ACCOUNT   | Azure storage account name   |
| PUBLISH_STORAGE_CONTAINER | Azure storage container name |
| PUBLISH_STORAGE_KEY       | Azure storage access key     |

Above variables are required for publish to be attempted.

### Tasks

| Name                                | Description                                          | Default            |
|-------------------------------------|------------------------------------------------------|--------------------|
| Setup                               | Sets up shared build data                            |                    |
| Clean                               | Cleans artifacts, bin and obj directories            |                    |
| Restore                             | Restores NuGet packages                              |                    |
| Build                               | Builds solution                                      |                    |
| Publish                             | Publishes project to artifacts directory             |                    |
| ClickOnce-Launcher                  | Creates ClickOnce launcher                           |                    |
| ClickOnce-Application-Manifest      | Creates ClickOnce application manifest               |                    |
| ClickOnce-Deployment-Manifest       | Creates ClickOnce deployment manifest                | Yes                |
| ClickOnce-Deployment-UpdateManifest | Updates ClickOnce deployment manifest min version    |                    |
| ClickOnce-Deployment-CreateAppRef   | Creates ClickOnce app ref file                       |                    |
| ClickOnce-Upload-Version            | Uploads new version to blob storage                  |                    |
| ClickOnce-Upload-Application        | Uploads application manifest to point to new version | On GitHub Actions  |

## Getting started

### Requirements

* Windows
* .NET 6 SDK
* Cake 2.0 or newer [Cake.Tool](https://www.nuget.org/packages/Cake.Tool)

### Step-by-step

From command line in repo root

#### First time

1. Create tool manifest to tool versions are pinned/versioned with in repo
    * `dotnet new tool-manifest`
1. Install Cake tool
    * `dotnet tool install Cake.Tool`
1. Create Cake script as `build.cake` assuming application solution is in folder `./src` (check NuGet for latest version of recipe [Cake.ClickOnce.Recipe](https://www.nuget.org/packages/Cake.ClickOnce.Recipe)).

```csharp
#load "nuget:?package=Cake.ClickOnce.Recipe&version=0.6.0"

ClickOnce.ApplicationName = "MyApp";
ClickOnce.Publisher = "PublisherName";
ClickOnce.PublishUrl = "https://{storageAccount}.blob.core.windows.net/{container}";
ClickOnce.RunBuild();
```

4. Execute script
  * `dotnet cake`

If all succeeds, it should have build and publish the application to `./artifacts` directory, if running on GitHub actions it will also deploy to the configured Azure Blob Storage.

## Resources

* Blog post - [Introducing Cake ClickOnce Recipe](https://www.devlead.se/posts/2021/2021-03-03-introducing-cake-clickonce-recipe)
* Recipe on NuGet.org - [nuget.org/packages/Cake.ClickOnce.Recipe](https://www.nuget.org/packages/Cake.ClickOnce.Recipe/)
* Example repository on GitHub - [github.com/devlead/Cake.ClickOnce.Recipe.Example](https://github.com/devlead/Cake.ClickOnce.Recipe.Example)
* Cake - [cakebuild.net](https://cakebuild.net/)
