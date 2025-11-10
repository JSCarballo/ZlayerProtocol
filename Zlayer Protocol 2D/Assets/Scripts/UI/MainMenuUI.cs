using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Botones")]
    public Button playButton;
    public Button continueButton;   // visible pero deshabilitado (según tu regla actual)
    public Button quitButton;

    [Header("Raíz visual del botón Continuar (opcional)")]
    public GameObject continueButtonRoot;

    [Header("Opcional: foco inicial")]
    public Selectable firstSelected;

    void OnEnable()
    {
        ForceDisableContinue();
        if (firstSelected) firstSelected.Select();
    }

    void Start()
    {
        ForceDisableContinue();
    }

    void ForceDisableContinue()
    {
        if (continueButton) continueButton.interactable = false;
        if (continueButtonRoot) continueButtonRoot.SetActive(true);
    }

    public void OnClickPlay()
    {
        // Nueva partida: limpiar cualquier estado previo
        SaveManager.ClearSave();

        // Si persiste FloorFlowController u otros managers, piden reset.
        if (FloorFlowController.Instance != null)
        {
            FloorFlowController.Instance.ResetRunState();
        }

        // Arranque en frío: destruye DDOL, libera recursos y carga Game desde limpio
        SceneLoader.LoadGameColdBoot();
    }

    public void OnClickContinue()
    {
        // Botón deshabilitado por tu flujo actual; sin efecto.
    }

    public void OnClickQuit()
    {
        SceneLoader.QuitGame();
    }
}
