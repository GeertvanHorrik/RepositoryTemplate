#l "components-variables.cake"

#addin "nuget:?package=Cake.FileHelpers&version=3.0.0"

//-------------------------------------------------------------

private bool HasComponents()
{
    return Components != null && Components.Length > 0;
}

//-------------------------------------------------------------

private void UpdateInfoForComponents()
{
    if (!HasComponents())
    {
        return;
    }

    foreach (var component in Components)
    {
        Information("Updating version for component '{0}'", component);

        var projectFileName = string.Format("./src/{0}/{0}.csproj", component);

        TransformConfig(projectFileName, new TransformationCollection 
        {
            { "Project/PropertyGroup/PackageVersion", VersionNuGet }
        });
    }
}

//-------------------------------------------------------------

private void BuildComponents()
{
    if (!HasComponents())
    {
        return;
    }
    
    foreach (var component in Components)
    {
        Information("Building component '{0}'", component);

        var projectFileName = string.Format("./src/{0}/{0}.csproj", component);
        
        var msBuildSettings = new MSBuildSettings {
            Verbosity = Verbosity.Quiet, // Verbosity.Diagnostic
            ToolVersion = MSBuildToolVersion.VS2017,
            Configuration = ConfigurationName,
            MSBuildPlatform = MSBuildPlatform.x86, // Always require x86, see platform for actual target platform
            PlatformTarget = PlatformTarget.MSIL
        };

        // TODO: Enable GitLink / SourceLink, see RepositoryUrl, RepositoryBranchName, RepositoryCommitId variables

        MSBuild(projectFileName, msBuildSettings);
    }
}

//-------------------------------------------------------------

private void PackageComponents()
{
    if (!HasComponents())
    {
        return;
    }

    foreach (var component in Components)
    {
        Information("Packaging component '{0}'", component);

        var projectFileName = string.Format("./src/{0}/{0}.csproj", component);

        // Note: we have a bug where UAP10.0 cannot be packaged, for details see 
        // https://github.com/dotnet/cli/issues/9303
        // 
        // Therefore we will use VS instead for packing and lose the ability to sign
        var useDotNetPack = true;

        var projectFileContents = FileReadText(projectFileName);
        if (!string.IsNullOrWhiteSpace(projectFileContents))
        {
            useDotNetPack = !projectFileContents.ToLower().Contains("uap10.0");
        }

        if (useDotNetPack)
        {
            var packSettings = new DotNetCorePackSettings
            {
                Configuration = ConfigurationName,
                NoBuild = true,
            };

            DotNetCorePack(projectFileName, packSettings);
        }
        else
        {
            Warning("Using Visual Studio to pack instead of 'dotnet pack' because UAP 10.0 project was detected. Unfortunately assemblies will not be signed inside the NuGet package");

            var msBuildSettings = new MSBuildSettings 
            {
                Verbosity = Verbosity.Minimal, // Verbosity.Diagnostic
                ToolVersion = MSBuildToolVersion.VS2017,
                Configuration = ConfigurationName,
                MSBuildPlatform = MSBuildPlatform.x86, // Always require x86, see platform for actual target platform
                PlatformTarget = PlatformTarget.MSIL
            };

            msBuildSettings.Properties["ConfigurationName"] = new List<string>(new [] { ConfigurationName });
            msBuildSettings.Properties["PackageVersion"] = new List<string>(new [] { VersionNuGet });
            msBuildSettings = msBuildSettings.WithTarget("pack");

            MSBuild(projectFileName, msBuildSettings);
        }
    }

    var codeSign = (!IsCiBuild && !string.IsNullOrWhiteSpace(CodeSignCertificateSubjectName));
    if (codeSign)
    {
        // For details, see https://docs.microsoft.com/en-us/nuget/create-packages/sign-a-package
        // nuget sign MyPackage.nupkg -CertificateSubjectName <MyCertSubjectName> -Timestamper <TimestampServiceURL>
        var filesToSign = GetFiles(string.Format("{0}/*.nupkg", OutputRootDirectory));

        foreach (var fileToSign in filesToSign)
        {
            Information("Signing NuGet package '{0}'", fileToSign);

            var exitCode = StartProcess(NuGetExe, new ProcessSettings
            {
                Arguments = string.Format("sign \"{0}\" -CertificateSubjectName \"{1}\" -Timestamper \"{2}\"", fileToSign, CodeSignCertificateSubjectName, CodeSignTimeStampUri)
            });

            Information("Signing NuGet package exited with '{0}'", exitCode);
        }
    }
}

//-------------------------------------------------------------

Task("UpdateInfoForComponents")
    .IsDependentOn("Clean")
    .Does(() =>
{
    UpdateSolutionAssemblyInfo();
    UpdateInfoForComponents();
});

//-------------------------------------------------------------

Task("BuildComponents")
    .IsDependentOn("UpdateInfoForComponents")
    .Does(() =>
{
    BuildComponents();
});

//-------------------------------------------------------------

Task("PackageComponents")
    .IsDependentOn("BuildComponents")
    .Does(() =>
{
    PackageComponents();
});