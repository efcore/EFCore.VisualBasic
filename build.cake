var target = Argument<string>("target");
var configuration = Argument<string>("configuration");
var signOutput = HasArgument("signOutput");

Task("Build")
    .Does(
        ctx =>
        {
            var settings = new DotNetCoreBuildSettings
            {
                Configuration = configuration,
                MSBuildSettings = new DotNetCoreMSBuildSettings
                {
                    MaxCpuCount = 0,
                    NoLogo = true,
                    Properties =
                    {
                        { "SignOutput", new[] { signOutput.ToString() } }
                    }
                }
            };

            if (signOutput)
            {
                var properties = settings.MSBuildSettings.Properties;

                var nugetExePath = ctx.Environment.GetSpecialPath(SpecialPath.LocalTemp).CombineWithFilePath("nuget.exe");
                DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", nugetExePath);
                properties.Add("NuGetExePath", new[] { nugetExePath.FullPath });

                var windowsSdkKey = ctx.Registry.LocalMachine.OpenKey(@"SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows\v10.0");
                var windowsSdkDir = windowsSdkKey.GetValue("InstallationFolder");
                var windowsSDKVersion = windowsSdkKey.GetValue("ProductVersion");
                properties.Add("SigntoolDir", new[] { $@"{windowsSdkDir}bin\{windowsSDKVersion}.0\x86\" });
            }

            DotNetCoreBuild("EFCore.VisualBasic.sln", settings);
        });

Task("Test")
    .IsDependentOn("Build")
    .Does(
        () =>
            DotNetCoreTest(
                "EFCore.VisualBasic.Test/EFCore.VisualBasic.Test.vbproj",
                new DotNetCoreTestSettings
                {
                    Configuration = configuration,
                    NoBuild = true,
                    NoRestore = true
                }));

RunTarget(target);
