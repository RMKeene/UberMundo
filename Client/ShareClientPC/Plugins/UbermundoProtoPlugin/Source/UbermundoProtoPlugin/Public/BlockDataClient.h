// Copyright Bahnda 2020, All rights reserved.

// This is the low level interface to the World Block Data.
// Each player owns their data and it resides in the file system on their machine. (Or possibly a 
// network shared file system like DropBox).
// It is styled as an async file system, with open, close, read, write of text and/or binary and/or other objects
// in the future.

#pragma once

#include "CoreMinimal.h"
#include "Containers/HashTable.h"
#include "Containers/List.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include <time.h>
#include "BlockDataClient.generated.h"


DECLARE_LOG_CATEGORY_EXTERN(ShareAssetIOCategory, Log, All);

#define SFIO_MAX_FPS 20

UENUM(BlueprintType)
enum SharedRequestStatus {
	/** This Shared Request has not been initialized yet. */
	InvalidRequest UMETA(DisplayName = "Invalid Request"),
	/** No request is outstanding */
	Idle UMETA(DisplayName = "Idle"),
	Pending UMETA(DisplayName = "Pending"),
	Success UMETA(DisplayName = "Success"),
	Failed UMETA(DisplayName = "Failed")
};

UENUM(BlueprintType)
enum ShareObjectTypes {
	share_obj_none,
	share_read_block_state,
	share_put_block_state,
	share_read_block_state_binary,
	share_put_block_state_binary
};


UCLASS()
class UBERMUNDOPROTOPLUGIN_API UShareRequest : public UObject {
	GENERATED_BODY()
public:
	static int64 nextRequestHandle;
	int requestHandle;
	TEnumAsByte<SharedRequestStatus> status;
	FString fail_reason;
	ShareObjectTypes share_obj_type;
	/** When this request was created.  Lets us reap forgotton stale requests. */
	time_t  creationTimestamp;

	UShareRequest() {
		requestHandle = nextRequestHandle++;
		status = InvalidRequest;
		share_obj_type = share_obj_none;
		fail_reason = "";
		time(&creationTimestamp);
	}

	~UShareRequest() {
		status = InvalidRequest;
		share_obj_type = share_obj_none;
		fail_reason = "";
		time(&creationTimestamp);
	}
};

UCLASS()
class UBERMUNDOPROTOPLUGIN_API UShareGetBlockState : public UShareRequest {
	GENERATED_BODY()
public:

	FString blockState;
	int8* blockStateBinary = NULL;
	size_t blockSize = 0;

	UShareGetBlockState() : UShareRequest() {
		blockState = FString("");
		share_obj_type = share_read_block_state;
	}

	~UShareGetBlockState() {
		blockState = FString("");
		share_obj_type = share_read_block_state;
		if (blockStateBinary != NULL) {
			free(blockStateBinary);
			blockStateBinary = NULL;
		}
	}
};

UCLASS()
class UBERMUNDOPROTOPLUGIN_API USharePutBlockState : public UShareRequest {
	GENERATED_BODY()
public:

	FString blockState;
	int8* blockStateBinary = NULL;
	size_t blockSize = 0;

	USharePutBlockState() : UShareRequest() {
		blockState = FString("");
		share_obj_type = share_put_block_state;
	}

	~USharePutBlockState() {
		blockState = FString("");
		share_obj_type = share_read_block_state;
		if (blockStateBinary != NULL) {
			free(blockStateBinary);
			blockStateBinary = NULL;
		}
	}

};

/**
 *
 */
UCLASS()
class UBERMUNDOPROTOPLUGIN_API UBlockDataClient : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	/** Open a file in the raw C++ fopen style.
		Returns an integer handle representing the file.
		The handle is NOT a pointer.
	*/
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO", meta = (ToolTip = "Start a request in the background to fetch a Share Block state as a string."))
		static void RequestShareBlock(FString blockPathAndName, int64& requestHandle, bool& success);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO", meta = (ToolTip = "Start a request in the background to fetch a Share Block state as a string."))
		static void RequestShareBlockBinary(FString blockPathAndName, int64& requestHandle, bool& success);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO", meta = (ToolTip = "Start a request in the background to put a Share Block state as a string."))
		static void WriteShareBlock(FString blockPathAndName, int64& requestHandle, bool& success, FString contents);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO", meta = (ToolTip = "Start a request in the background to put a Share Block state as a string."))
		static void WriteShareBlockBinary(FString blockPathAndName, TArray<uint8> contents, int64& requestHandle, bool& success);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO")
		static void CancelShareBlockRequest(int64 requestHandle, bool& success);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO")
		static void CloseShareBlockRequest(int64 requestHandle, bool& success);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "UberMundo Asset IO")
		static void GetShareBlockRequestStatus(int64 requestHandle, TEnumAsByte<SharedRequestStatus>& status, FString& failReason);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO", meta = (ToolTip = "Once Get Share Block Request Status returns Success you can call this to get the data."))
		static FString GetShareBlockResults(int64 requestHandle, bool& success);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset IO", meta = (ToolTip = "Once Get Share Block Request Status returns Success you can call this to get the data."))
		static void GetShareBlockResultsBinary(int64 requestHandle, bool& success, TArray<uint8>& contents, FString& errorReason);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "UberMundo Asset IO")
		static UClass* FindClassByStringName(FString ClassName);

	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset Helpers", meta = (ToolTip = "List the Blueprint CLasses."))
		static bool ListAllBlueprintsInPath(FName Path, TArray<UClass*>& Result, UClass* Class);
	UFUNCTION(BlueprintCallable, Category = "UberMundo Asset Helpers", meta = (ToolTip = "List the assets. Each string is type then path like \"Blueprint /Game/DefaultObjects/BigBox.BigBox\"/"))
		static bool ListAllAssetsInPath(FString Path, UClass* Class, TArray<FString>& Result);

private:
	static TMap<int64, UShareRequest*> outstanding_requests;

	/** Assumes fromIdx is on a EOL or at the start of s.
	Moves forward to the next EOL, or end of string.
	If fromIdx is at the end of string, returns -1.
	ONLY '\n' is a valid EOL, never checks for '\r'.
	*/
	static int FindNextEOL(FString& s, int fromIdx);
	/** Gets the next line of text (can be zero length). If at end success is false, else true.
	  fromIdx gets set to the new EOL location, or end of string, or -1 on done. */
	static FString ExtractNextLine(FString& s, int& fromIdx, bool& success);



};

