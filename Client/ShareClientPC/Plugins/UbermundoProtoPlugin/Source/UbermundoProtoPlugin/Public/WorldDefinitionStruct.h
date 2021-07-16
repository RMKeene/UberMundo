#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "WorldDefinitionStruct.generated.h"

USTRUCT(BlueprintType)
struct FWorldDefinitionStruct
{
	GENERATED_USTRUCT_BODY()

	FWorldDefinitionStruct() : 
		WorldName("No Name"), 
		Owner(0), 
		WotToEnter(0.1f), 
		Version(1), 
		PlayerUpdateFactor(1.0f), 
		NextObjectId(1) {
	}

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		FString WorldName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		int64 Owner;
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		float WotToEnter;
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		int Version;
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		float PlayerUpdateFactor;
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		TArray<FString> SearchWords;
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
		int NextObjectId;

};