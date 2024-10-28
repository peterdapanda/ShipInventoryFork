﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using ShipInventoryFork.Helpers;
using Unity.Netcode;
using UnityEngine;
using Logger = ShipInventoryFork.Helpers.Logger;

namespace ShipInventoryFork.Objects;

public class ChuteInteract : NetworkBehaviour
{
    public static ChuteInteract? Instance = null;

    public override void OnNetworkSpawn()
    {
        _trigger = GetComponent<InteractTrigger>();
        itemRestorePoint = transform.Find("DropNode");
        spawnParticles = GetComponentInChildren<ParticleSystem>();

        // Add Rigidbody and Collider to make the object movable
        var rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = false; // Disable gravity if you don't want it to fall
        rigidbody.isKinematic = false; // Enable physics-based movement

        var collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = false; // Disable trigger to make it interactable

        base.OnNetworkSpawn();
        gameObject.AddComponent<DraggableObject>();
    }

    
    /// <summary>
    /// Updates the value of the chute
    /// </summary>
    public void UpdateValue()
    {
        var grabbable = GetComponent<GrabbableObject>();
        
        // Skip if item invalid
        if (grabbable == null)
            return;
        
        grabbable.scrapValue = ItemManager.GetTotalValue();
        grabbable.OnHitGround(); // Update 
    }

    #region Store Items
    
    public void StoreHeldItem(PlayerControllerB player)
    {
        GrabbableObject item = player.currentlyHeldObjectServer;
        
        // If item invalid, skip
        if (item is null)
        {
            Logger.Info($"Player '{player.playerUsername}' is not holding any item.");
            return;
        }
        
        // Send store request to server
        ItemManager.StoreItem(item);
        
        // Update scrap collected
        item.isInShipRoom = false;
        item.scrapPersistedThroughRounds = true; // stfu collect pop up
        player.SetItemInElevator(true, true, item);
        
        // Despawn the held item
        Logger.Debug("Despawn held object...");
        player.DespawnHeldObject();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StoreItemServerRpc(ItemData data)
    {
        Logger.Debug("Server received new item!");
        Logger.Debug("Sending new item to clients...");
        StoreItemClientRpc(data);
    }

    [ClientRpc]
    private void StoreItemClientRpc(ItemData data)
    {
        Logger.Debug("Client received new item!");
        ItemManager.Add(data);
        Logger.Debug("Client added new item!");
    }

    #endregion
    #region Spawn Items

    private Transform itemRestorePoint = null!;
    private ParticleSystem spawnParticles = null!;
    
    private readonly Queue<ItemData> spawnQueue = [];
    private Coroutine? spawnCoroutine;
    
    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemServerRpc(ItemData data, int count = 1)
    {
        var item = data.GetItem();
        
        if (item is null)
            return;

        var items = ItemManager.GetInstances(data, count);
        foreach (var itemData in items)
            spawnQueue.Enqueue(itemData);

        spawnCoroutine ??= StartCoroutine(SpawnCoroutine());
        Logger.Debug($"Server scheduled to spawn {items.Count()} new items!");
    }

    [ClientRpc]
    public void SpawnItemClientRpc(NetworkObjectReference networkObject, ItemData data)
    {
        Logger.Debug("Updating the items...");
        ItemManager.Remove(data);
        Logger.Debug("Items updated!");
        
        var item = data.GetItem();

        if (!networkObject.TryGet(out var obj) || item == null)
            return;

        var grabObj = obj.GetComponent<GrabbableObject>();
        
        // Set up object
        if (item.isScrap)
            grabObj.SetScrapValue(data.SCRAP_VALUE);

        grabObj.scrapPersistedThroughRounds = data.PERSISTED_THROUGH_ROUNDS;
            
        if (item.saveItemVariable)
            grabObj.LoadItemSaveData(data.SAVE_DATA);

        grabObj.isInShipRoom = true;
        grabObj.isInElevator = true;
        grabObj.StartCoroutine(PlayDropSound(grabObj));

        // Play particles
        spawnParticles.Play();
        
        Logger.Debug("Item setup!");
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator SpawnCoroutine()
    {
        // Spawn each item
        while (spawnQueue.Count > 0)
        {
            // If chute is full, skip
            if (itemsInChute.Length >= ShipInventoryFork.Config.StopAfter.Value)
            {
                yield return new WaitForEndOfFrame();
                continue;
            }
            
            var data = spawnQueue.Dequeue();
            var item = data.GetItem();
        
            if (item == null)
                continue;
        
            var newItem = Instantiate(item.spawnPrefab) ?? throw new NullReferenceException();
            newItem.transform.SetParent(itemRestorePoint, false);
        
            // Set values
            var grabObj = newItem.GetComponent<GrabbableObject>();
            grabObj.transform.localPosition = Vector3.zero;

            if (grabObj.itemProperties.itemSpawnsOnGround)
                grabObj.transform.localRotation = Quaternion.Euler(grabObj.itemProperties.restingRotation);
            else
                grabObj.OnHitGround();
        
            // Spawn item
            var networkObj = grabObj.NetworkObject;
            networkObj.Spawn();
        
            SpawnItemClientRpc(networkObj, data);
            
            yield return new WaitForSeconds(ShipInventoryFork.Config.SpawnDelay.Value);
        }

        // Mark as completed
        spawnCoroutine = null;
    }

    private static IEnumerator PlayDropSound(GrabbableObject grabbable)
    {
        yield return null;
        yield return null;
        grabbable.PlayDropSFX();
        grabbable.OnHitGround();
    }
    
    #endregion
    #region Request Items

    public void RequestItems()
    {
        Logger.Debug("Requesting the items to the server...");
        RequestItemsServerRpc(GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    public void RequestItemsAll()
    {
        // Skip if request from client
        if (GameNetworkManager.Instance.localPlayerController.IsClient)
            return;

        var ids = StartOfRound.Instance.allPlayerScripts
            .Where(p => p.IsClient)
            .Select(p => p.playerClientId);
        
        RequestItemsServerRpc(ids.ToArray());
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestItemsServerRpc(params ulong[] ids)
    {
        Logger.Debug("Item request heard!");
        Logger.Debug($"Sending the items to client {string.Join(", ", ids)}...");
        RequestItemsClientRpc(ItemManager.GetItems().ToArray(), new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ids
            }
        });
    }

    [ClientRpc]
    private void RequestItemsClientRpc(ItemData[] data, ClientRpcParams @params = default)
    {
        Logger.Debug("Client received items!");
        
        // Skip if target is invalid
        var targets = @params.Send.TargetClientIds ?? [];
        
        // If not a target, skip
        if (!targets.Contains(GameNetworkManager.Instance.localPlayerController.playerClientId))
            return;

        ItemManager.SetItems(data.ToList());
        Logger.Debug("Client updated items!");
    }

    #endregion
    #region Trigger

    private Collider[] itemsInChute = [];
    private InteractTrigger _trigger = null!;

    private void UpdateTrigger()
    {
        // If no trigger, skip
        if (!_trigger)
            return;
        
        if (GameNetworkManager.Instance is null)
            return;
        
        if (NetworkManager.Singleton is null)
            return;
        
        var local = GameNetworkManager.Instance.localPlayerController;
        
        // If player invalid, skip
        if (local is null)
            return;
        
        // If player outside the ship, skip
        if (!local.isInHangarShipRoom)
            return;

        // Update interactable
        ItemManager.UpdateTrigger(_trigger, local);

        // Update layer
        // ReSharper disable once Unity.PreferNonAllocApi
        itemsInChute = Physics.OverlapBox(
            itemRestorePoint.position,
            new Vector3(1f, 0.25f, 1.25f) / 2,
            itemRestorePoint.rotation,
            1 << LayerMask.NameToLayer(Constants.LAYER_PROPS)
        );
        
        gameObject.layer = LayerMask.NameToLayer(itemsInChute.Length > 0 ? Constants.LAYER_IGNORE : Constants.LAYER_INTERACTABLE);
    }

    #endregion
    #region MonoBehaviour

    private void Update() => UpdateTrigger();

    #endregion
}