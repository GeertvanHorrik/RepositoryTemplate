#l "generic-variables.cake"

#addin "nuget:?package=MagicChunks&version=2.0.0.119"
#addin "nuget:?package=Cake.FileHelpers&version=3.0.0"

//-------------------------------------------------------------

private void UpdateSolutionAssemblyInfo()
{
    Information("Updating assembly info to '{0}'", VersionFullSemVer);

    var assemblyInfoParseResult = ParseAssemblyInfo(SolutionAssemblyInfoFileName);

    var assemblyInfo = new AssemblyInfoSettings {
        Company = assemblyInfoParseResult.Company,
        Version = VersionMajorMinorPatch,
        FileVersion = VersionMajorMinorPatch,
        InformationalVersion = VersionFullSemVer,
        Copyright = string.Format("Copyright © {0} {1} - {2}", Company, StartYear, DateTime.Now.Year)
    };

    CreateAssemblyInfo(SolutionAssemblyInfoFileName, assemblyInfo);
}

//-------------------------------------------------------------

Task("UpdateNuGet")
    .ContinueOnError()
    .Does(() => 
{
    Information("Making sure NuGet is using the latest version");

    var exitCode = StartProcess(NuGetExe, new ProcessSettings
    {
        Arguments = "update -self"
    });

    var newNuGetVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(NuGetExe);
    var newNuGetVersion = newNuGetVersionInfo.FileVersion;

    Information("Updating NuGet.exe exited with '{0}', version is '{1}'", exitCode, newNuGetVersion);
});

//-------------------------------------------------------------

Task("RestorePackages")
    .IsDependentOn("UpdateNuGet")
    .Does(() =>
{
    var solutions = GetFiles("./**/*.sln");
    
    foreach(var solution in solutions)
    {
        Information("Restoring packages for {0}", solution);
        
        var nuGetRestoreSettings = new NuGetRestoreSettings();

        if (!string.IsNullOrWhiteSpace(NuGetPackageSources))
        {
            var sources = new List<string>();

            foreach (var splitted in NuGetPackageSources.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                sources.Add(splitted);
            }
            
            if (sources.Count > 0)
            {
                nuGetRestoreSettings.Source = sources;
            }
        }

        NuGetRestore(solution, nuGetRestoreSettings);
    }
});

//-------------------------------------------------------------

// Note: it might look weird that this is dependent on restore packages,
// but to clean, the msbuild projects must be able to load. However, they need
// some targets files that come in via packages

Task("Clean")
    .IsDependentOn("RestorePackages")
    .ContinueOnError()
    .Does(() => 
{
    if (DirectoryExists(OutputRootDirectory))
    {
        DeleteDirectory(OutputRootDirectory, new DeleteDirectorySettings()
        {
            Force = true,
            Recursive = true
        });
    }

    var platforms = new Dictionary<string, PlatformTarget>();
    platforms["AnyCPU"] = PlatformTarget.MSIL;
    platforms["x86"] = PlatformTarget.x86;
    platforms["x64"] = PlatformTarget.x64;
    platforms["arm"] = PlatformTarget.ARM;

    foreach (var platform in platforms)
    {
        Information("Cleaning output for platform '{0}'", platform.Value);

        MSBuild(SolutionFileName, configurator => 
            configurator.SetConfiguration(ConfigurationName)
                .SetVerbosity(Verbosity.Minimal)
                .SetMSBuildPlatform(MSBuildPlatform.x86)
                .SetPlatformTarget(platform.Value)
                .WithTarget("Clean"));
    }
});

//-------------------------------------------------------------

Task("CodeSign")
    .ContinueOnError()
    .Does(() =>
{
    if (IsCiBuild)
    {
        Information("Skipping code signing because this is a CI build");
        return;
    }

    if (string.IsNullOrWhiteSpace(CodeSignCertificateSubjectName))
    {
        Information("Skipping code signing because the certificate subject name was not specified");
        return;
    }

    var exeSignFilesSearchPattern = OutputRootDirectory + string.Format("/**/*{0}*.exe", CodeSignWildCard);
    var dllSignFilesSearchPattern = OutputRootDirectory + string.Format("/**/*{0}*.dll", CodeSignWildCard);

    List<FilePath> filesToSign = new List<FilePath>();

    Information("Searching for files to code sign using '{0}'", exeSignFilesSearchPattern);

    filesToSign.AddRange(GetFiles(exeSignFilesSearchPattern));

    Information("Searching for files to code sign using '{0}'", dllSignFilesSearchPattern);

    filesToSign.AddRange(GetFiles(dllSignFilesSearchPattern));

    Information("Found '{0}' files to code sign, this can take a few minutes", filesToSign.Count);

    var signToolSignSettings = new SignToolSignSettings 
    {
        AppendSignature = false,
        TimeStampUri = new Uri(CodeSignTimeStampUri),
        CertSubjectName = CodeSignCertificateSubjectName
    };

    Sign(filesToSign, signToolSignSettings);

    // Note parallel doesn't seem to be faster in an example repository:
    // 1 thread:   1m 30s
    // 4 threads:  1m 30s
    // 10 threads: 1m 30s
    // Parallel.ForEach(filesToSign, new ParallelOptions 
    //     { 
    //         MaxDegreeOfParallelism = 10 
    //     },
    //     fileToSign => 
    //     { 
    //         Sign(fileToSign, signToolSignSettings);
    //     });
});
