using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ColdBootLoader : MonoBehaviour
{
    [Header("Escena objetivo")]
    public string gameSceneName = "Game";

    [Header("Opcional: logs de debug")]
    public bool verbose = false;

    void Start()
    {
        StartCoroutine(BootCoroutine());
    }

    IEnumerator BootCoroutine()
    {
        if (verbose) Debug.Log("[ColdBoot] Iniciando arranque en frío...");

        // 1) Estado base seguro
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 2) Destruir TODO lo que esté en DontDestroyOnLoad
        if (verbose) Debug.Log("[ColdBoot] Limpiando DontDestroyOnLoad...");
        CleanDontDestroyOnLoad();

        // 3) Unframe para que se procese la destrucción
        yield return null;

        // 4) Liberar recursos no usados
        if (verbose) Debug.Log("[ColdBoot] Resources.UnloadUnusedAssets...");
        yield return Resources.UnloadUnusedAssets();

        // 5) GC para soltar memoria
        if (verbose) Debug.Log("[ColdBoot] GC.Collect...");
        System.GC.Collect();
        yield return null;

        // 6) Cargar Game FRESCO
        if (verbose) Debug.Log("[ColdBoot] Cargando escena de juego...");
        SceneManager.LoadScene(string.IsNullOrEmpty(gameSceneName) ? "Game" : gameSceneName, LoadSceneMode.Single);
    }

    void CleanDontDestroyOnLoad()
    {
        // Truco: crear un GO temporal, moverlo a DDOL, y obtener su Scene especial
        var probe = new GameObject("DDOL_Probe");
        DontDestroyOnLoad(probe);
        var ddolScene = probe.scene;

        var roots = new List<GameObject>();
#if UNITY_2021_3_OR_NEWER
        ddolScene.GetRootGameObjects(roots);
#else
        roots.AddRange(ddolScene.GetRootGameObjects());
#endif
        // Destruir TODO lo que esté en DDOL, excepto el probe
        foreach (var go in roots)
        {
            if (go == probe) continue;
            if (go) Destroy(go);
        }

        // Destruir el probe también
        Destroy(probe);
    }
}
