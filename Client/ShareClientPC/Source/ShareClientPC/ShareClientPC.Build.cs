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
		// Ubermundo does all its own Steam API control and calls.
		//PrivateDependencyModuleNames.Add("OnlineSubsystem");

	}
}
