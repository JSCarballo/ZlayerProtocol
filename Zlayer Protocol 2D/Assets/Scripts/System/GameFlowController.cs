using UnityEngine;
using System.Collections;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("UI Roots")]
    public GameObject pauseMenuRoot;
    public GameObject deathScreenRoot;
    public GameObject victoryScreenRoot;

    [Header("Opcional: bloquear audio al pausar")]
    public bool pauseAudioListener = true;

    bool isPaused = false;
    bool isDeadOrVictory = false;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Time.timeScale = 1f;
        SetActiveSafe(pauseMenuRoot, false);
        SetActiveSafe(deathScreenRoot, false);
        SetActiveSafe(victoryScreenRoot, false);

        isPaused = false;
        isDeadOrVictory = false;
    }

    void Update()
    {
        if (isDeadOrVictory) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseFromEsc();
        }
    }

    void SetActiveSafe(GameObject go, bool v) { if (go && go.activeSelf != v) go.SetActive(v); }

    // -------- PAUSA ----------
    public void TogglePauseFromEsc()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseAudioListener) AudioListener.pause = true;
        SetActiveSafe(pauseMenuRoot, true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseAudioListener) AudioListener.pause = false;
        SetActiveSafe(pauseMenuRoot, false);
    }

    public void UI_OnPause_Continue() => ResumeGame();

    public void UI_OnPause_ExitToMenu()
    {
        StartCoroutine(SaveAndExitToMenu_Co());
    }

    IEnumerator SaveAndExitToMenu_Co()
    {
        // Guarda partida activa
        SaveManager.SaveActiveRun("Game");
        yield return null; // asegurar estado

        isPaused = false;
        isDeadOrVictory = false;
        Time.timeScale = 1f;
        if (pauseAudioListener) AudioListener.pause = false;

        SceneLoader.LoadMainMenu();
    }

    // -------- DEATH ----------
    public void OnPlayerDied()
    {
        if (isDeadOrVictory) return;
        isDeadOrVictory = true;
        Time.timeScale = 0f;
        if (pauseAudioListener) AudioListener.pause = true;
        SetActiveSafe(pauseMenuRoot, false);
        SetActiveSafe(deathScreenRoot, true);

        // Dejamos Continuar disponible si el usuario decide salir al menú
        SaveManager.SaveActiveRun("Game");
    }

    // Reiniciar = flujo de "Jugar": limpia y carga CoolBoot
    public void UI_OnDeath_Restart()
    {
        SaveManager.ClearSave(); // nueva partida
        isDeadOrVictory = false;
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseAudioListener) AudioListener.pause = false;

        // Mismo pipeline que el botón Jugar del menú principal
        SceneLoader.LoadGameColdBoot();
    }

    public void UI_OnDeath_ExitToMenu()
    {
        StartCoroutine(SaveAndExitToMenu_Co());
    }

    // -------- VICTORY ----------
    public void OnVictory()
    {
        if (isDeadOrVictory) return;
        isDeadOrVictory = true;
        Time.timeScale = 0f;
        if (pauseAudioListener) AudioListener.pause = true;
        SetActiveSafe(pauseMenuRoot, false);
        SetActiveSafe(victoryScreenRoot, true);

        // En victoria se suele iniciar nueva run al reiniciar
        // (si quisieras permitir continuar, comenta la siguiente línea)
        SaveManager.ClearSave();
    }

    // Reiniciar = flujo de "Jugar": limpia y carga CoolBoot
    public void UI_OnVictory_Restart()
    {
        // Aseguramos nueva partida
        SaveManager.ClearSave();

        isDeadOrVictory = false;
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseAudioListener) AudioListener.pause = false;

        // Mismo pipeline que el botón Jugar del menú principal
        SceneLoader.LoadGameColdBoot();
    }

    public void UI_OnVictory_ExitToMenu()
    {
        // Tras victoria, ir al menú (ya limpiamos arriba)
        Time.timeScale = 1f;
        if (pauseAudioListener) AudioListener.pause = false;
        SceneLoader.LoadMainMenu();
    }

    void OnApplicationQuit()
    {
        // Si se cierra el juego en medio de la partida, dejamos continuar.
        if (!isDeadOrVictory) SaveManager.SaveActiveRun("Game");
    }
}
