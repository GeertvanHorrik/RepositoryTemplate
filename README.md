# RepositoryTemplate

Template repository with scripts using Cake Build. This repository template is regularly updated with new tweaks to improve compilation for any apps supported.

The scripts are set up so only the `build.cake` in the root of the repository contains project-specific information and needs customization.

# Features

- Local and build server variables
	- Continua CI
- Run cleanup / restore at start
- Auto determine versioning (via build server or fallback to GitVersion)
- Build any project
	-  Libraries (components)
	-  WPF apps
	-  UWP apps
		- No native compilation to speed up build times
 	 	- Dynamic switching between ARM and ARM64 will be implemented
	-  Web apps
	-  Docker containers
- SonarQube integration (optional)
- SourceLink integration (even if not added to a project, it will be done for you dynamically)
- Code signing (if a code signing certificate is available)
- Deploy any project
	- Libraries (push to a NuGet (or MyGet) feed)
	- WPF apps (using Squirrel.Windows)
	- UWP apps (automatic deployments to the Microsoft Store)
	- Web apps (using Octopus Deploy)
- Include / exclude patterns for faster builds

The builds are fully automated and can be ran from either Powershell or command prompt.

The source code is easily extendible to custom needs (e.g. support for different build servers).

# Setting up the scripts

## Copy the files 

The first time, you will need to copy the files from `/replace/` to your repository.

In subsequent updates, you can simply run `/run.ps1`, it will replace all the files.

## Creating build.cake

To create a build script, use the following default template in the root of the repository:

```
//=======================================================
// DEFINE PARAMETERS
//=======================================================

// Define the required parameters
var Parameters = new Dictionary<string, object>();
Parameters["SolutionName"] = "[ProjectName, e.g. MyProject]";
Parameters["Company"] = "[CompanyName, e.g. MyCompany]";
Parameters["RepositoryUrl"] = string.Format("https://github.com/{0}/{1}", GetBuildServerVariable("Company"), GetBuildServerVariable("SolutionName"));
Parameters["StartYear"] = "[StartYear, e.g. 2014]";
Parameters["UseVisualStudioPrerelease"] = "false";

// Note: the rest of the variables should be coming from the build server,
// see `/deployment/cake/*-variables.cake` for customization options
// 
// If required, more variables can be overridden by specifying them via the 
// Parameters dictionary, but the build server variables will always override
// them if defined by the build server. For example, to override the code
// sign wild card, add this to build.cake
//
// Parameters["CodeSignWildcard"] = "Orc.EntityFramework";

//=======================================================
// DEFINE COMPONENTS TO BUILD / PACKAGE
//=======================================================

// TODO: Define components, apps, etc to build / package

//Components.Add(string.Format("{0}", GetBuildServerVariable("SolutionName")));
//WpfApps.Add(string.Format("{0}", GetBuildServerVariable("SolutionName")));
//UwpApps.Add(string.Format("{0}", GetBuildServerVariable("SolutionName")));
//WebApps.Add(string.Format("{0}", GetBuildServerVariable("SolutionName")));
//DockerImages.Add(string.Format("{0}", GetBuildServerVariable("SolutionName")));
//TestProjects.Add(string.Format("{0}.Tests", GetBuildServerVariable("SolutionName")));

//=======================================================
// REQUIRED INITIALIZATION, DO NOT CHANGE
//=======================================================

// Now all variables are defined, include the tasks, that
// script will take care of the rest of the magic

#l "./deployment/cake/tasks.cake"
```

Once the default template is in place in the root of the repository, specific projects can be added.

## Using with Components

### Variables required

*Note that these variables could (or should?) be coming from the build server / agent if possible. If that's not possible, then add the following variables to the top of your `build.cake` file.* 

```
Parameters["NuGetRepositoryUrl"] = "";
Parameters["NuGetRepositoryApiKey"] = "";
```

### Define components

```
Components.Add("MyComponent");
```

## Using with WPF apps

### Variables required

*Note that these variables could (or should?) be coming from the build server / agent if possible. If that's not possible, then add the following variables to the top of your `build.cake` file.* 

[TODO]

### Define components

```
WpfApps.Add("MyWpfApp");
```

## Using with UWP apps

### Variables required

*Note that these variables could (or should?) be coming from the build server / agent if possible. If that's not possible, then add the following variables to the top of your `build.cake` file.* 

```
Parameters["WindowsStoreAppId"] = "";
Parameters["WindowsStoreClientId"] = "";
Parameters["WindowsStoreClientSecret"] = "";
Parameters["WindowsStoreTenantId"] = "";
```

### Define components

```
UwpApps.Add("MyUwpProject");
```

## Using with Web apps

### Variables required

*Note that these variables could (or should?) be coming from the build server / agent if possible. If that's not possible, then add the following variables to the top of your `build.cake` file.* 

[TODO]

### Define components

```
WebApps.Add("MyWebApp");
```

## Using with Docker images

### Variables required

*Note that these variables could (or should?) be coming from the build server / agent if possible. If that's not possible, then add the following variables to the top of your `build.cake` file.* 

[TODO]

### Define components

```
DockerImages.Add("MyDockerImage");
```

## Using with test projects

### Variables required

None

### Define components

```
TestProjects.Add("MyProject.Tests");
```

# Running builds

There are several target actions available which can be ran in stages from a build server and/or agent:

- UpdateInfo => update the version info
- Build => build all the components in release mode (unless it's a local build, then it's done in debug)
- Test => run all tests in the test projects
- Package => packages the components / apps into deployable packages
- PackageLocal => packages but also updates / replaces local NuGet packages with the same version in the NuGet cache 
- Deploy

There are also some convenience combinations:

- BuildAndTest
- BuildAndPackage
- BuildAndPackageLocal
- BuildAndDeploy
- Default (BuildAndPackage) 

## Running a build with local packages

To run a local build that is referencable, use the following script using powershell or command prompt:

```
.\build.ps1 -target buildandpackagelocal
```

## Running a build for a specific project

```
.\build.ps1 -target buildandpackagelocal -include MyOnlyComponentToInclude
```

## Running a build for everything but a specific project

```
.\build.ps1 -target buildandpackagelocal -exclude MyOnlyComponentToExclude
```

## Build and deploy it all

```
.\build.ps1 -target buildanddeploy
```

# Detailed explanation of all variables

TODO: Create a table with all variables