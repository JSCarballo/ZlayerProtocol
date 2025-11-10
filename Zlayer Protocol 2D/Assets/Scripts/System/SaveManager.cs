// Assets/Scripts/System/SaveManager.cs
using UnityEngine;

public static class SaveManager
{
    // Claves PlayerPrefs
    const string KEY_HAS_SAVE = "HAS_ACTIVE_SAVE";
    const string KEY_SAVED_SCENE = "SAVED_SCENE";
    const string KEY_SAVED_TIME = "SAVED_TIME";
    const string KEY_SAVE_VER = "SAVE_VERSION";

    // Versión simple por si cambias el formato luego
    const int CURRENT_VERSION = 1;

    public static bool HasSave()
    {
        // Debe existir flag y versión válida
        if (PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) != 1) return false;
        if (PlayerPrefs.GetInt(KEY_SAVE_VER, 0) != CURRENT_VERSION) return false;

        // Debe tener una escena guardada (por ahora "Game")
        var scene = PlayerPrefs.GetString(KEY_SAVED_SCENE, "");
        return !string.IsNullOrEmpty(scene);
    }

    public static void SaveActiveRun(string sceneName = "Game")
    {
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.SetInt(KEY_SAVE_VER, CURRENT_VERSION);
        PlayerPrefs.SetString(KEY_SAVED_SCENE, string.IsNullOrEmpty(sceneName) ? "Game" : sceneName);
        PlayerPrefs.SetString(KEY_SAVED_TIME, System.DateTime.Now.ToString("O"));
        PlayerPrefs.Save();
        // Debug.Log("[SaveManager] Guardado activo");
    }

    public static void ClearSave()
    {
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 0);
        PlayerPrefs.DeleteKey(KEY_SAVED_SCENE);
        PlayerPrefs.DeleteKey(KEY_SAVED_TIME);
        PlayerPrefs.SetInt(KEY_SAVE_VER, CURRENT_VERSION);
        PlayerPrefs.Save();
        // Debug.Log("[SaveManager] Save limpiado");
    }

    public static string GetSavedScene() => PlayerPrefs.GetString(KEY_SAVED_SCENE, "Game");
}
