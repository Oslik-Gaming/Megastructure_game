using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages persistence of trash removal state across game sessions.
/// Uses PlayerPrefs to track which trash items have been removed.
/// </summary>
public static class TrashPersistence
{
    private const string KeyPrefix = "Trash_";
    private const string KeyGeneration = "TrashGeneration";

    /// <summary>Current generation number. Incremented when starting a new game.</summary>
    private static int CurrentGeneration => PlayerPrefs.GetInt(KeyGeneration, 0);

    /// <summary>
    /// Check if a trash item with the given ID has been removed.
    /// </summary>
    public static bool IsRemoved(string persistentId)
    {
        if (string.IsNullOrEmpty(persistentId))
            return false;

        string key = BuildKey(persistentId);
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Mark a trash item as removed.
    /// </summary>
    public static void MarkAsRemoved(string persistentId)
    {
        if (string.IsNullOrEmpty(persistentId))
            return;

        string key = BuildKey(persistentId);
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Reset all trash persistence data. Called when starting a new game.
    /// </summary>
    public static void ResetAllTrash()
    {
        // Increment generation to invalidate all previous trash keys
        int newGeneration = CurrentGeneration + 1;
        PlayerPrefs.SetInt(KeyGeneration, newGeneration);
        PlayerPrefs.Save();

        Debug.Log($"[TrashPersistence] Reset all trash. New generation: {newGeneration}");
    }

    /// <summary>
    /// Build a unique key for a trash item including scene name and generation.
    /// </summary>
    private static string BuildKey(string persistentId)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return $"{KeyPrefix}{sceneName}_{persistentId}_Gen{CurrentGeneration}";
    }

#if UNITY_EDITOR
    /// <summary>
    /// Debug method to clear all trash persistence data.
    /// </summary>
    [UnityEditor.MenuItem("Tools/Clear All Trash Persistence")]
    public static void ClearAllTrashData()
    {
        PlayerPrefs.DeleteKey(KeyGeneration);
        
        // Note: This doesn't delete individual trash keys, but incrementing generation
        // effectively invalidates them all. For a complete cleanup, use PlayerPrefs.DeleteAll().
        
        Debug.Log("[TrashPersistence] Cleared trash generation counter.");
    }
#endif
}
