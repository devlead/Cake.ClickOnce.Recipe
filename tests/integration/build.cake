#load "nuget:%RECIPE_SOURCE%?package=Cake.ClickOnce.Recipe&version=%RECIPE_VERSION%"

ClickOnce.ApplicationName = "MyApp";
ClickOnce.Publisher = "devlead";
ClickOnce.PublishUrl = "https://bloburi/publish";
ClickOnce.RunBuild();