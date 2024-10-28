﻿using GameNetcodeStuff;
using HarmonyLib;
using ShipInventoryFork.Objects;

namespace ShipInventoryFork.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
public class PlayerControllerB_Patches
{
    /// <summary>
    /// Loads all the items and requests the items from the host
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    private static void RequestOnConnect(PlayerControllerB __instance)
    {
        ShipInventoryFork.PrepareItems();
        
        // If host, skip
        if (__instance.IsHost)
            return;

        ChuteInteract.Instance?.RequestItems();
    }
}