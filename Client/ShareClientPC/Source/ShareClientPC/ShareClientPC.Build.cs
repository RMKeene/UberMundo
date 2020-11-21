// Fill out your copyright notice in the Description page of Project Settings.

using UnrealBuildTool;

public class ShareClientPC : ModuleRules
{
	public ShareClientPC(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
	
		PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "InputCore" });

		PrivateDependencyModuleNames.AddRange(new string[] {  });

		// Uncomment if you are using Slate UI
		// PrivateDependencyModuleNames.AddRange(new string[] { "Slate", "SlateCore" });

		// Uncomment if you are using online features
		//PrivateDependencyModuleNames.Add("OnlineSubsystem");

		// To include OnlineSubsystemSteam, add it to the plugins section in your uproject file with the Enabled attribute set to true
		string steamLibPath = @"SteamSDKFiles\redistributable_bin\win64";
		PublicSystemLibraryPaths.Add(steamLibPath);
		PublicAdditionalLibraries.Add("steam_api64.lib");
	}
}
