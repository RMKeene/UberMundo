// Some copyright should be here...

using UnrealBuildTool;

public class UbermundoProtoPlugin : ModuleRules
{
    public UbermundoProtoPlugin(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicIncludePaths.AddRange(
            new string[] {
				// ... add public include paths required here ...
            }
            );


        PrivateIncludePaths.AddRange(
            new string[] {
				// ... add other private include paths required here ...
			}
            );


        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core",
				// ... add other public dependencies that you statically link with here ...
			}
            );


        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "CoreUObject",
                "Engine",
                "Slate",
                "SlateCore",
				// ... add private dependencies that you statically link with here ...	
			}
            );


        DynamicallyLoadedModuleNames.AddRange(
            new string[]
            {
				// ... add any modules that your module loads dynamically here ...
            }
            );

        string steamLib = ModuleDirectory + @"\..\..\SteamSDKFiles\redistributable_bin\win64\steam_api64.lib";
        string steamDLL = ModuleDirectory + @"\..\..\SteamSDKFiles\redistributable_bin\win64\steam_api64.dll";
        System.Console.WriteLine("UbermundoProtoPlugin.Build.cs: *************** Current Directory of Build:  " + System.IO.Directory.GetCurrentDirectory());
        System.Console.WriteLine("UbermundoProtoPlugin.Build.cs: *************** ModuleDirectory: " + ModuleDirectory);
        System.Console.WriteLine("UbermundoProtoPlugin.Build.cs: *************** Steam lib: " + steamLib);
        //PublicDelayLoadDLLs.Add(ModuleDirectory + @"\..\..\SteamSDKFiles\redistributable_bin\steam_api64.dll");
        PublicAdditionalLibraries.Add(steamLib);
    }
}
