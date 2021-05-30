// Copyright 2020 Bahnda. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "steam/steam_api.h"
#include "SteamCustomCode.generated.h"

DECLARE_LOG_CATEGORY_EXTERN(UberMundoSteamLog, Log, All);

class ShareSteamCallbackHooks {
public:
	ShareSteamCallbackHooks();
	// These auto-register as callbacks on object createion of this ShareSteamCallbackHooks
	STEAM_CALLBACK(ShareSteamCallbackHooks, OnP2PSessionRequest, P2PSessionRequest_t, m_CallbackP2PSessionRequest);
	STEAM_CALLBACK(ShareSteamCallbackHooks, OnP2PSessionConnectFail, P2PSessionConnectFail_t, m_CallbackP2PSessionConnectFail);
};

/**
 *
 */
UCLASS()
class UBERMUNDOPROTOPLUGIN_API USteamCustomCode : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:


	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "SteamSDK_Init"))
		static bool InitSteam();
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "SteamSDK_Shutdown"))
		static bool ShutdownSteam();
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "SteamSDK IsSteamRunning"))
		static bool IsSteamRunning();

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Gets local Steam ID as an 8 byte array."))
		static bool IsLocalSteamID(int64 id, bool ifNoSteam);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Gets local Steam ID as an 8 byte array."))
		static TArray<uint8> GetLocalSteamID();
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (WorldContext = "WorldContextObject", ToolTip = "Gets local Steam ID as an 8 byte array. If in editor, returns a dummy Steam ID for PIE mode. 0xFF01010100000000 hexadecimal = 18374969058454929408 unsigned decimal. Intentionaly uses the high bits FF to test for sign problems."))
		static TArray<uint8> GetLocalSteamIDSafe(UObject* WorldContextObject);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Gets local Steam ID as an Int64."))
		static int64 GetLocalSteamIDInt64();
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (WorldContext = "WorldContextObject", ToolTip = "Gets local Steam ID as an Int64. If in editor, returns a dummy Steam ID for PIE mode. 0xFF01010100000000 hexadecimal = 18374969058454929408 unsigned decimal. Intentionaly uses the high bits FF to test for sign problems."))
		static int64 GetLocalSteamIDSafeInt64(UObject* WorldContextObject);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "SteamUser()->GetSteamName()"))
		static FString GetLocalSteamName();
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "SteamUser()->GetSteamName()"))
		static FString GetRemoteSteamName(TArray<uint8> steamUserID);

	/// <summary>
	/// Must be called every game tick.  This is the SteamAPI_RunCallbacks Call.
	/// </summary>
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "SteamAPI_RunCallbacks : Should be called once every tick of the game."))
		static bool Tick();

	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "Send UDP Now. If not connected or routed yet, drops the packet.  MAX 1200 bytes."))
		static bool SendP2PPacket_UnreliableNoDelay(TArray<uint8> targetUserSteamId, TArray<uint8> bytes);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "Send via normal UDP. Will wait for the connection to be valid.  MAX 1200 bytes."))
		static bool SendP2PPacket_Unreliable(TArray<uint8> targetUserSteamId, TArray<uint8> bytes);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "Max 1 MB. Does reassembly and ordering of packets from fragments."))
		static bool SendP2PPacket_Reliable(TArray<uint8> targetUserSteamId, TArray<uint8> bytes);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "Max 1 MB. Does reassembly and ordering of packets from fragments. Will accumulate multiple packets over a max of 200 ms. for more efficient send."))
		static bool SendP2PPacket_ReliableWithBuffered(TArray<uint8> targetUserSteamId, TArray<uint8> bytes);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Is there data to get? Returns 0 if no data."))
		static int IsP2PPacketAvailable();
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "Get a packet of data.  If no packets returns false."))
		static bool ReadP2PPacket(TArray<uint8>& bytes, TArray<uint8>& remoteSteamID);

	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "Drop conversations with remote user."))
		static bool CloseP2PBySteamID(TArray<uint8> remoteSteamID);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "True if is valid ID. This means Universe, Instance, Type, and Unique 32 bit ID are set."))
		static bool IsValidSteamID(TArray<uint8> steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the account type bits of the steam ID. 1 is k_EAccountTypeIndividual, 3 is k_EAccountTypeGameServer"))
		static int GetSteamIDAccountType(TArray<uint8> steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the universe bits of the steam ID. 1 is k_EUniversePublic (normal accounts)"))
		static int GetSteamIDUniverse(TArray<uint8> steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the instance bits of the steam ID. Always 1"))
		static int GetSteamIDInstance(TArray<uint8> steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the account type bits of the steam ID. The unique account ID, lower 32 bits of staemID."))
		static int GetSteamIDAccountID(TArray<uint8> steamID);

private:
	// Helpers for to and from uint64 to 8 byte array.
	static uint64 FromBytes(TArray<uint8> a);
	static TArray<uint8> ToBytes(uint64 i);
};
