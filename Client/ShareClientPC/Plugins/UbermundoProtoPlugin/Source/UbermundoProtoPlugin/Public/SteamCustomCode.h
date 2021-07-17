// Copyright 2020 Bahnda. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "steam/steam_api.h"
#include "SteamCustomCode.generated.h"

DECLARE_LOG_CATEGORY_EXTERN(UberMundoSteamLog, Log, All);

UENUM(BlueprintType)
enum EFriendFlagsUM {

	EFriendFlagNoneUM = 0x00,
	EFriendFlagBlockedUM = 0x01,
	EFriendFlagFriendshipRequestedUM = 0x02,
	EFriendFlagImmediateUM = 0x04,			// "regular" friend
	EFriendFlagClanMemberUM = 0x08,
	EFriendFlagOnGameServerUM = 0x10,

	// EFriendFlagHasPlayedWithUM	= 0x20,	// not currently used
	// EFriendFlagFriendOfFriendUM	= 0x40, // not currently used

	EFriendFlagRequestingFriendshipUM = 0x80,
	EFriendFlagRequestingInfoUM = 0x100,
	EFriendFlagIgnoredUM = 0x200,
	EFriendFlagIgnoredFriendUM = 0x400,

	// EFriendFlagSuggestedUM		= 0x800,	// not used

	EFriendFlagChatMember = 0x1000,
	EFriendFlagAll = 0xFFFF
};

UENUM(BlueprintType)
enum EPersonaStateUM
{
	EPersonaStateOfflineUM = 0,			// friend is not currently logged on
	EPersonaStateOnlineUM = 1,			// friend is logged on
	EPersonaStateBusyUM = 2,			// user is on, but busy
	EPersonaStateAwayUM = 3,			// auto-away feature
	EPersonaStateSnoozeUM = 4,			// auto-away for a long time
	EPersonaStateLookingToTradeUM = 5,	// Online, trading
	EPersonaStateLookingToPlayUM = 6,	// Online, wanting to play
	EPersonaStateInvisibleUM = 7,		// Online, but appears offline to friends.  This status is never published to clients.
	EPersonaStateMaxUM,
};

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
		static FString GetRemoteSteamName(int64 steamUserID);

	/// <summary>
	/// Must be called every game tick.  This is the SteamAPI_RunCallbacks Call.
	/// </summary>
	UFUNCTION(BlueprintCallable, Category = "ShareSteam", meta = (ToolTip = "SteamAPI_RunCallbacks : Should be called once every tick of the game."))
		static bool Tick();

	UFUNCTION(BlueprintCallable, Category = "ShareSteam|P2P", meta = (ToolTip = "Send UDP Now. If not connected or routed yet, drops the packet.  MAX 1200 bytes."))
		static bool SendP2PPacket_UnreliableNoDelay(int64 targetUserSteamId, TArray<uint8> bytes);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|P2P", meta = (ToolTip = "Send via normal UDP. Will wait for the connection to be valid.  MAX 1200 bytes."))
		static bool SendP2PPacket_Unreliable(int64 targetUserSteamId, TArray<uint8> bytes);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|P2P", meta = (ToolTip = "Max 1 MB. Does reassembly and ordering of packets from fragments."))
		static bool SendP2PPacket_Reliable(int64 targetUserSteamId, TArray<uint8> bytes);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|P2P", meta = (ToolTip = "Max 1 MB. Does reassembly and ordering of packets from fragments. Will accumulate multiple packets over a max of 200 ms. for more efficient send."))
		static bool SendP2PPacket_ReliableWithBuffered(int64 targetUserSteamId, TArray<uint8> bytes);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam|P2P", meta = (ToolTip = "Is there data to get? Returns 0 if no data, else the number of bytes available."))
		static int IsP2PPacketAvailable();
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|P2P", meta = (ToolTip = "Get a packet of data.  If no packets returns false."))
		static bool ReadP2PPacket(TArray<uint8>& bytes, int64& remoteSteamID);

	UFUNCTION(BlueprintCallable, Category = "ShareSteam|P2P", meta = (ToolTip = "Drop conversations with remote user."))
		static bool CloseP2PBySteamID(int64 remoteSteamID);

	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "True if is valid ID. This means Universe, Instance, Type, and Unique 32 bit ID are set."))
		static bool IsValidSteamID(int64 steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the account type bits of the steam ID. 1 is k_EAccountTypeIndividual, 3 is k_EAccountTypeGameServer"))
		static int GetSteamIDAccountType(int64 steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the universe bits of the steam ID. 1 is k_EUniversePublic (normal accounts)"))
		static int GetSteamIDUniverse(int64 steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the instance bits of the steam ID. Always 1"))
		static int GetSteamIDInstance(int64 steamID);
	UFUNCTION(BlueprintCallable, BlueprintPure, Category = "ShareSteam", meta = (ToolTip = "Get the account type bits of the steam ID. The unique account ID, lower 32 bits of staemID."))
		static int GetSteamIDAccountID(int64 steamID);

	UFUNCTION(BlueprintCallable, Category = "ShareSteam|Friends", meta = (ToolTip = "Get a steam user's friends list size. -1 is not logged in, -2 is steam not initialized."))
		static int GetFriendCount(EFriendFlagsUM iFriendFlags);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|Friends", meta = (ToolTip = "Get a steam user's friends list friend SteamID."))
		static int64 GetFriendByIndex(int idx, EFriendFlagsUM iFriendFlags);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|Friends", meta = (ToolTip = "Get a steam user's friends list friend Name."))
		static FString GetFriendPersonalName(int64 steamID);
	UFUNCTION(BlueprintCallable, Category = "ShareSteam|Friends", meta = (ToolTip = "Get a steam user's friends list friend PersonaState, e.g. Online. "))
		static EPersonaStateUM GetFriendPersonaState(int64 steamID);

private:
	// Helpers for to and from uint64 to 8 byte array. C++ code only, not blueprint.
	static uint64 FromBytes(TArray<uint8> a);
	static TArray<uint8> ToBytes(uint64 i);
};
