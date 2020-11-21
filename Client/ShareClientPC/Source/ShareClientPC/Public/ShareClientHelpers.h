// Copyright 2020 Bahnda. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"


#include "ShareClientHelpers.generated.h"

/**
 * 
 */
UCLASS()
class SHARECLIENTPC_API UShareClientHelpers : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
	
public:

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "Guid", meta = (ToolTip = "Convert 128 bit Guid to 16 bytes."))
		static void GetGuidBytes(FGuid guid, 
			uint8& B0, uint8& B1, uint8& B2, uint8& B3,
			uint8& B4, uint8& B5, uint8& B6, uint8& B7,
			uint8& B8, uint8& B9, uint8& B10, uint8& B11,
			uint8& B12, uint8& B13, uint8& B14, uint8& B15);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "Guid", meta = (ToolTip = "16 bytes into a Guid"))
		static FGuid GuidFromBytes(uint8 B0, 
			uint8 B1, uint8 B2, uint8 B3,
			uint8 B4, uint8 B5, uint8 B6, uint8 B7,
			uint8 B8, uint8 B9, uint8 B10, uint8 B11,
			uint8 B12, uint8 B13, uint8 B14, uint8 B15);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "Share Helpers", meta = (ToolTip = "Convert 64 bit integer64 to 8 bytes. (Big Endian, B0 is MSB)"))
		static void GetLongBytes(int64 i, 
			uint8& B0, uint8& B1, uint8& B2, uint8& B3,
			uint8& B4, uint8& B5, uint8& B6, uint8& B7);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "Share Helpers", meta = (ToolTip = "8 bytes into a integer64. (Big Endian, B0 is MSB)"))
		static int64 LongFromBytes(uint8 B0,
			uint8 B1, uint8 B2, uint8 B3,
			uint8 B4, uint8 B5, uint8 B6, uint8 B7);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "Share Helpers", meta = (ToolTip = "Get a setting in [ShareSettings] in DefaultGame.ini"))
		static FString GetGameSetting(FString key, FString defaultReturn);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "Share Helpers", meta = (ToolTip = "integer64 to hex"))
		static FString Int64ToHex(int64 i);

};
