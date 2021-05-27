// Fill out your copyright notice in the Description page of Project Settings.


#include "BlockDataClient.h"
#include "GameFramework/Actor.h"
#include "Misc/DefaultValueHelper.h"
#include "AssetRegistryModule.h"
#include <direct.h>
#include <Programs\UnrealHeaderTool\Private\ParserClass.h>
#include <Runtime\Engine\Classes\Engine\ObjectLibrary.h>


	/** If defined then do firect disk file I/O for local debug.
		If not defined use Steam async net messages to the content server or some other user that has the data.
	*/
#define DO_DIRECT_FILE_IO 1
	//#define DO_TCP 1

	DEFINE_LOG_CATEGORY(ShareAssetIOCategory)

		TMap<int64, UShareRequest*> UBlockDataClient::outstanding_requests;
	int64 UShareRequest::nextRequestHandle = 1;

	void UBlockDataClient::RequestShareBlock(FString blockPathAndName, int64& requestHandle, bool& success) {
		requestHandle = -1;
		success = false;

		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("RequestShareBlock %s"), *blockPathAndName);
		UShareGetBlockState* s = NewObject<UShareGetBlockState>();
		s->status = SharedRequestStatus::Pending;
		outstanding_requests.Add(s->requestHandle, s);
		requestHandle = s->requestHandle;
		success = true;
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("RequestShareBlock %s (handle is %ld)"), *blockPathAndName, requestHandle);

#ifdef DO_TCP

#endif

#ifdef DO_DIRECT_FILE_IO
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("RequestShareBlock %s (handle is %ld) - Direct local file read."), *blockPathAndName, requestHandle);
		FILE* fp = fopen(TCHAR_TO_UTF8(*blockPathAndName), "r");
		if (fp == NULL) {
			s->status = SharedRequestStatus::Failed;
			s->fail_reason = FString(UTF8_TO_TCHAR(strerror(errno)));
			UE_LOG(ShareAssetIOCategory, Error, TEXT("RequestShareBlock %s (handle is %ld) - %s"), *blockPathAndName, requestHandle, *(s->fail_reason));
			return;
		}
		fseek(fp, 0L, SEEK_END);
		size_t sz = ftell(fp);
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("data size %ld"), sz);
		rewind(fp);
		char* buf = (char*)malloc(sz + 1);
		fread(buf, sz, 1, fp);
		buf[sz] = '\0';
		s->blockState = FString(UTF8_TO_TCHAR(buf));
		s->status = SharedRequestStatus::Success;
		fclose(fp);
		free(buf);
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("RequestShareBlock %s (handle is %ld) - Direct local file read. OK"), *blockPathAndName, requestHandle);
#endif
	}

	void UBlockDataClient::CancelShareBlockRequest(int64 requestHandle, bool& success) {
		success = false;
		if (outstanding_requests.Contains(requestHandle)) {
			outstanding_requests.Remove(requestHandle);
			success = true;
		}
	}

	void UBlockDataClient::WriteShareBlock(FString blockPathAndName, int64& requestHandle, bool& success, FString blockState) {
		requestHandle = -1;
		success = false;

		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("WriteShareBlock %s"), *blockPathAndName);
		USharePutBlockState* s = NewObject<USharePutBlockState>();
		s->status = SharedRequestStatus::Pending;
		s->blockState = blockState;
		outstanding_requests.Add(s->requestHandle, s);
		requestHandle = s->requestHandle;
		success = true;
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("WriteShareBlock %s (handle is %ld)"), *blockPathAndName, requestHandle);

#ifdef DO_DIRECT_FILE_IO
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("WriteShareBlock %s (handle is %ld) - Direct local file read."), *blockPathAndName, requestHandle);
		FILE* fp = fopen(TCHAR_TO_UTF8(*blockPathAndName), "w");
		if (fp == NULL) {
			s->status = SharedRequestStatus::Failed;
			s->fail_reason = FString(UTF8_TO_TCHAR(strerror(errno)));
			UE_LOG(ShareAssetIOCategory, Error, TEXT("WriteShareBlock %s (handle is %ld) - %s"), *blockPathAndName, requestHandle, *(s->fail_reason));
			return;
		}

		// blockstate is FString and in Unicode, 2 bytes per character.
		// We want it as single byte UTF8
		// This is brute force and probably not very platform portable.
		// Also it will fail for non-ascii characters such as chinese.
		int sz = blockState.Len();
		char* buf = (char*)malloc(sz);
		const TCHAR* bufUni = *blockState;
		for (int i = 0; i < sz; i++) {
			buf[i] = bufUni[i];
		}
		size_t nwritten = fwrite(buf, 1, sz, fp);
		if (nwritten == sz) {
			s->status = SharedRequestStatus::Success;
		}
		else {
			s->status = SharedRequestStatus::Failed;
			s->fail_reason = FString(UTF8_TO_TCHAR(strerror(errno)));
		}
		fclose(fp);
		// Do NOT free bufUni, it is owned by the FString blockState!
		free(buf);
		UE_LOG(ShareAssetIOCategory, Verbose, TEXT("WriteShareBlock %s (handle is %ld) - Direct local file read. OK"), *blockPathAndName, requestHandle);
#endif
	}

	void UBlockDataClient::CloseShareBlockRequest(int64 requestHandle, bool& success) {
		CancelShareBlockRequest(requestHandle, success);
	}


	void UBlockDataClient::GetShareBlockRequestStatus(int64 requestHandle, TEnumAsByte<SharedRequestStatus>& status, FString& failReason) {
		status = SharedRequestStatus::InvalidRequest;
		failReason = "";
		if (outstanding_requests.Contains(requestHandle)) {
			status = outstanding_requests[requestHandle]->status;
			failReason = outstanding_requests[requestHandle]->fail_reason;
		}
		else {
			status = SharedRequestStatus::InvalidRequest;
			failReason = "No such request handle.";
		}
	}

	FString UBlockDataClient::GetShareBlockResults(int64 requestHandle, bool& success) {
		success = false;
		if (!outstanding_requests.Contains(requestHandle)) {
			return "No such request";
		}

		UShareRequest* req = outstanding_requests[requestHandle];
		if (req->status != SharedRequestStatus::Success) {
			return "Request not in 'success' state.";
		}

		if (req->share_obj_type != share_read_block_state) {
			return "Request not a Share Block State request.";
		}

		return ((UShareGetBlockState*)req)->blockState;
	}

	int UBlockDataClient::FindNextEOL(FString& s, int fromIdx) {
		if (fromIdx >= s.Len() || fromIdx == -1) {
			return -1;
		}

		// If s does not start with EOL (End of Line) and at the start of s, in effect s[-1] is a virtual EOL.
		if (fromIdx != 0 || s[fromIdx] != '\n') {
			fromIdx++;
		}
		while (fromIdx < s.Len()) {

			if (s[fromIdx] == '\n') {
				return fromIdx;
			}
			fromIdx++;
		}
		return s.Len();
	}

	FString UBlockDataClient::ExtractNextLine(FString& s, int& fromIdx, bool& success) {
		// Trailing \n at end of s check.
		if (fromIdx == s.Len() - 1 || fromIdx == -1) {
			success = false;
			fromIdx = -1;
			return FString("");
		}
		int nextEolIdx = FindNextEOL(s, fromIdx);
		if (nextEolIdx == -1) {
			success = false;
			fromIdx = -1;
			return FString("");
		}
		int strt = fromIdx + 1;
		// If s does not start with EOL (End of Line) and at the start of s, in effect s[-1] is a virtual EOL.
		if (fromIdx == 0 && s[fromIdx] != '\n') {
			strt = fromIdx;
		}
		fromIdx = nextEolIdx;
		success = true;
		return s.Mid(strt, nextEolIdx - strt);
	}

	UClass* UBlockDataClient::FindClassByStringName(FString ClassName)
	{
		UObject* ClassPackage = ANY_PACKAGE;

		if (UClass* Result = FindObject<UClass>(ClassPackage, *ClassName))
			return Result;

		if (UObjectRedirector* RenamedClassRedirector = FindObject<UObjectRedirector>(ClassPackage, *ClassName))
			return CastChecked<UClass>(RenamedClassRedirector->DestinationObject);

		return nullptr;
	}

	bool UBlockDataClient::ListAllBlueprintsInPath(FName Path, TArray<UClass*>& Result, UClass* Class)
	{
		auto Library = UObjectLibrary::CreateLibrary(Class, true, GIsEditor);
		Library->LoadBlueprintAssetDataFromPath(Path.ToString());

		TArray<FAssetData> Assets;
		Library->GetAssetDataList(Assets);

		for (auto& Asset : Assets)
		{
			UBlueprint* bp = Cast<UBlueprint>(Asset.GetAsset());
			if (bp)
			{
				auto gc = bp->GeneratedClass;
				if (gc)
				{
					Result.Add(gc);
				}
			}
			else
			{
				auto GeneratedClassName = (Asset.AssetName.ToString() + "_C");

				UClass* Clazz = FindObject<UClass>(Asset.GetPackage(), *GeneratedClassName);
				if (Clazz)
				{
					Result.Add(Clazz);
				}
				else
				{
					UObjectRedirector* RenamedClassRedirector = FindObject<UObjectRedirector>(Asset.GetPackage(), *GeneratedClassName);
					if (RenamedClassRedirector)
					{
						Result.Add(CastChecked<UClass>(RenamedClassRedirector->DestinationObject));
					}
				}
			}
		}

		return true;
	}

	bool UBlockDataClient::ListAllAssetsInPath(FString Path, UClass* Class, TArray<FString>& Result)
	{
		UObjectLibrary* Library = UObjectLibrary::CreateLibrary(Class, true, GIsEditor);

		TArray<FString> Paths;
		Paths.Add(Path);

		Library->LoadBlueprintAssetDataFromPaths(Paths);

		TArray<FAssetData> Assets;
		Library->GetAssetDataList(Assets);

		for (FAssetData& a : Assets)
		{
			Result.Add(a.GetFullName());
		}

		return true;
	}
