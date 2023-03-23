using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.IO.FileSystemTasks;

class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Solution] readonly Solution Solution;
    
    [GitVersion]
    readonly GitVersion GitVersion;

    [Parameter] 
    readonly string DockerImageName;
    
    [Parameter]
    readonly string DockerRegistry = "registry.hub.docker.com";

    Target Print => _ => _
        .Executes(() =>
        {
            Log.Information("GitVersion = {Value}", GitVersion.FullSemVer);
        });
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            RootDirectory.GlobDirectories("*/src/*/obj",
                    "*/src/*/bin",
                    "*/test/*/obj",
                    "*/test/*/bin")
                .ForEach(DeleteDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(_ => _.SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(InvokedTargets.Contains(Restore)));
        });
    
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(_ => _
                .SetConfiguration(Configuration.Release)
                .SetProjectFile(Solution)
                .SetNoBuild(InvokedTargets.Contains(Compile))
            );
        });

    Target BuildDocker => _ => _
        .DependsOn(Test)
        .Requires(() => DockerImageName)
        .Executes(() =>
        {
           DotNetTasks.DotNetPublish(_ => _
                .SetProject(Solution.GetProject("Stipps.CloudflareIpUpdater"))
                .SetConfiguration(Configuration.Release)
                .SetVersion(GitVersion.MajorMinorPatch)
                .SetProperty("ContainerImageName", $"\"{DockerImageName}\"")
                .SetProperty("ContainerImageTag", GitVersion.MajorMinorPatch)
                //.EnableSelfContained()
                .EnablePublishSingleFile()
                .SetProcessArgumentConfigurator(_ => _
                    .Add("--os linux")
                    .Add("--arch x64")
                    .Add("/t:PublishContainer"))
            );
        });
}
