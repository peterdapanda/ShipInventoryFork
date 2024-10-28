﻿namespace ShipInventoryFork;

public static class Constants
{
    // --- PATHS ---
    public const string SHIP_PATH = "Environment/HangarShip"; // Path for the ship

    // --- TAGS ---
    public const string STORED_ITEMS = "ShipInventoryForkItems"; // Key for the data of the mod
    
    // --- ASSETS ---
    public const string VENT_PREFAB = "VentChute"; // Name of the prefab
    public const string MOD_ICON = "icon"; // Name of the mod's icon
    public const string BUNDLE = "ShipInventoryFork.Resources.si-bundle"; // Name of the bundle
    
    // --- LAYERS ---
    public const string LAYER_PROPS = "Props"; // Name of the layer Props
    public const string LAYER_IGNORE = "Ignore Raycast"; // Name of the layer Ignore Raycast
    public const string LAYER_INTERACTABLE = "InteractableObject"; // Name of the layer Interactable Object
    
    // --- TERMINAL ---
    public const int ITEMS_PER_PAGE = 10;
}
