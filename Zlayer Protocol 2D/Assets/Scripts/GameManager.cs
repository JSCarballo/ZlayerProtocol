using UnityEngine;

/// <summary>
/// Manages game state, including starting the dungeon generation, spawning the
/// player and handling level transitions. This script should be placed on a
/// persistent GameObject in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public GameObject playerPrefab;
    public Transform playerSpawnParent;
    private GameObject playerInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartNewRun();
    }

    /// <summary>
    /// Starts a new game run by generating a dungeon and spawning the player
    /// at the origin room.
    /// </summary>
    public void StartNewRun()
    {
        // Clean up any existing rooms
        foreach (Transform child in dungeonGenerator.transform)
        {
            Destroy(child.gameObject);
        }
        dungeonGenerator.GenerateDungeon();
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerInstance != null) Destroy(playerInstance);
        playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity, playerSpawnParent);
    }
}