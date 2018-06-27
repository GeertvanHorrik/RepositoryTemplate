#l "generic-tasks.cake"
#l "apps-uwp-tasks.cake"
#l "apps-wpf-tasks.cake"
#l "components-tasks.cake"

#addin "nuget:?package=Cake.Sonar&version=1.1.0"

#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.3.0"

var Target = GetBuildServerVariable("Target", "Default");

Information("Running target '{0}'", Target);
Information("Using output directory '{0}'", OutputRootDirectory);

//-------------------------------------------------------------

Task("UpdateInfo")
    .Does(() =>
{
    UpdateSolutionAssemblyInfo();
    
    UpdateInfoForComponents();
    UpdateInfoForUwpApps();
    UpdateInfoForWpfApps();
});

//-------------------------------------------------------------

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("UpdateInfo")
    .Does(() =>
{
    var enableSonar = !string.IsNullOrWhiteSpace(SonarUrl);
    if (enableSonar)
    {
        SonarBegin(new SonarBeginSettings 
        {
            Url = SonarUrl,
            Login = SonarUsername,
            Password = SonarPassword,
            Verbose = false,
            Key = SonarProject
        });
    }
    else
    {
        Information("Skipping Sonar integration since url is not specified");
    }

    BuildComponents();
    BuildUwpApps();
    BuildWpfApps();

    if (!string.IsNullOrWhiteSpace(SonarUrl))
    {
        SonarEnd(new SonarEndSettings 
        {
            Login = SonarUsername,
            Password = SonarPassword,
        });
    }
});

//-------------------------------------------------------------

Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("CodeSign")
    .Does(() =>
{
    PackageComponents();
    PackageUwpApps();
    PackageWpfApps();
});

//-------------------------------------------------------------

Task("Default")
	.IsDependentOn("Build");

//-------------------------------------------------------------

RunTarget(Target);