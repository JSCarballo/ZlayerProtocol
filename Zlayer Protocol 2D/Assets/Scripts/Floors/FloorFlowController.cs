using UnityEngine;

public class FloorFlowController : MonoBehaviour
{
    public static FloorFlowController Instance { get; private set; }

    [Header("Secuencia de pisos")]
    public FloorSequenceSO sequence;

    [Header("Refs en escena")]
    public ProcDungeonGenerator generator;

    public int currentIndex { get; private set; } = -1;
    public FloorDefinitionSO Current =>
        (sequence && currentIndex >= 0 && currentIndex < sequence.floors.Count) ? sequence.floors[currentIndex] : null;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (!generator) generator = FindObjectOfType<ProcDungeonGenerator>();
    }

    void Start()
    {
        if (sequence == null || sequence.floors.Count == 0)
        {
            Debug.LogError("[FloorFlow] No hay FloorSequence asignada.");
            return;
        }
        NextFloor(); // inicia en el primero
    }

    public void NextFloor()
    {
        currentIndex++;
        if (currentIndex >= sequence.floors.Count)
        {
            WinGame();
            return;
        }

        // 1) Limpia UI/registro pero mantén subscriptores
        DungeonMapRegistry.Instance?.ClearAll();

        // 2) Limpia posibles restos de la escena anterior (ascensores, upgrades sueltos)
        CleanupSceneLeftovers();

        // 3) Genera
        if (!generator) generator = FindObjectOfType<ProcDungeonGenerator>();
        if (!generator)
        {
            Debug.LogError("[FloorFlow] No encuentro ProcDungeonGenerator en la escena.");
            return;
        }

        var cfg = sequence.floors[currentIndex];
        generator.GenerateForFloor(cfg);
        Debug.Log($"[FloorFlow] Entrando a piso {cfg.floorId} – {cfg.displayName}");
    }

    void CleanupSceneLeftovers()
    {
        // Ascensores
        var elevators = Object.FindObjectsByType<ElevatorExit>(FindObjectsSortMode.None);
        foreach (var e in elevators) if (e) Destroy(e.gameObject);

        // Upgrades sueltas (por si el jugador no las recogió)
        var upgrades = Object.FindObjectsByType<WeaponUpgradePickup>(FindObjectsSortMode.None);
        foreach (var u in upgrades) if (u) Destroy(u.gameObject);
    }

    public void WinGame()
    {
        Debug.Log("[FloorFlow] ¡Has vencido a la Reina! GAME CLEAR.");
        // TODO: UI de victoria / escena final
    }
}
