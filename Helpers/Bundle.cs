using UnityEngine;

namespace ShipInventoryFork.Helpers;

/// <summary>
/// Helper to load a bundle
/// </summary>
internal static class Bundle
{
    private static AssetBundle? loadedBundle;

    /// <summary>
    /// Tries to load the bundle with the given name
    /// </summary>
    /// <returns>Success of the load</returns>
    public static bool LoadBundle(string name)
    {
        var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

        if (stream == null)
        {
            // Removed Logger.Error
            return false;
        }

        loadedBundle = AssetBundle.LoadFromStream(stream);
        
        if (loadedBundle == null)
        {
            // Removed Logger.Error
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Tries to load the asset of the given name in the current bundle
    /// </summary>
    /// <returns>Asset loaded or null</returns>
    public static T? LoadAsset<T>(string name) where T : Object
    {
        if (loadedBundle == null)
        {
            // Removed Logger.Error
            return null;
        }

        var asset = loadedBundle.LoadAsset<T>(name);

        if (asset == null)
        {
            // Removed Logger.Error
        }
        
        return asset;
    }
}
