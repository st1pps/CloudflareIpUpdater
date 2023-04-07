using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
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
    
    [PathExecutable] Tool Dotnet;
    
    readonly AbsolutePath PublishDirectory = RootDirectory / "publish";

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
                    "*/test/*/bin",
                    "publish/*")
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
            var project = Solution.GetProject("Stipps.CloudflareIpUpdater.Daemon");
            
            // Had to switch to directly calling dotnet, because NUKE makes the workaround for multiple image tags impossible at the moment
            // https://github.com/dotnet/sdk-container-builds/issues/236
            
            var publishCommand = $"""
                    publish {project!.Path} --configuration Release /property:Version={GitVersion.MajorMinorPatch} /property:ContainerImageName={DockerImageName} /property:ContainerImageTags="\"{GitVersion.MajorMinorPatch};latest\"" /property:PublishSingleFile=True --os linux --arch x64 /t:PublishContainer
                """;

            Dotnet.Invoke(publishCommand);
            
            
        });

    Target PublishDocker => _ => _
        .DependsOn(BuildDocker)
        .Executes(() =>
        {
            var dockerUsername = Environment.GetEnvironmentVariable("DOCKER_USERNAME");
            var dockerPassword = Environment.GetEnvironmentVariable("DOCKER_PASSWORD");
            DockerTasks.DockerLogin(_ => _.SetUsername(dockerUsername).SetPassword(dockerPassword));
            DockerTasks.DockerPush(_ => _.EnableAllTags().SetName(DockerImageName));
            DockerTasks.DockerLogout();
        });
 
    Target PublishEndpoint => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            var project = Solution.GetProject("Stipps.CloudflareIpUpdater.DynDnsEndpoint");
            if (project is null) throw new Exception("Project not found");
            var outputDirectory = PublishDirectory / project.Name;
            DotNetTasks.DotNetPublish(_ => _
                .SetProject(project)
                .SetOutput(outputDirectory)
                .SetConfiguration(Configuration.Release)
                .SetNoBuild(InvokedTargets.Contains(Compile))
                .SetNoRestore(InvokedTargets.Contains(Restore))
                .DisableSelfContained()
                .SetRuntime("linux-x64")
            );
        });
        
        
}
