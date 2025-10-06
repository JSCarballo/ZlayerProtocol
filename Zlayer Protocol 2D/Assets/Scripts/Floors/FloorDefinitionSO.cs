// Assets/Scripts/Floors/FloorDefinitionSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "ZLayer/Floor Definition", fileName = "Floor_")]
public class FloorDefinitionSO : ScriptableObject
{
    [Header("Identidad")]
    public string floorId = "1";         // "1", "1B", "2", "3"...
    public string displayName = "Floor 1";

    [Header("Generación")]
    public int minRooms = 8;
    public int maxRooms = 12;
    [Range(0f, 0.8f)] public float branchChance = 0.25f;

    [Header("Extras (tipo TBOI)")]
    public bool bossIsExtra = true;     // Jefe en una hoja de 1 acceso
    public bool armoryIsExtra = true;   // Armory en hoja

    [Header("Encuentros (enemigos normales)")]
    public int enemyMin = 3;
    public int enemyMax = 6;

    [Header("Arena (piso 1B)")]
    public bool isArenaFloor = false;   // si true, SOLO la sala inicial arma oleadas
    public int arenaWaves = 3;
    public int arenaWaveMin = 6;
    public int arenaWaveMax = 10;
    public float arenaInterval = 1.0f;  // segundos entre oleadas

    [Header("Boss")]
    public bool hasBoss = true;
    public GameObject bossPrefab;
    public bool isFinalBoss = false;    // piso 3

    [Header("Prefabs comunes del piso")]
    public GameObject enemyPrefab;
    public GameObject doorBarrierPrefab;
    public GameObject upgradePickupPrefab; // Armory / drop boss
    public GameObject elevatorExitPrefab;  // aparece tras boss / arena

    [Header("Espaciado mundial entre salas")]
    public Vector2 roomWorldSpacing = new(20f, 14f);

    [Header("Prefabs de salas (por piso)")]
    [Tooltip("Si lo asignas, TODAS las salas de este piso usarán este RoomBuilder por defecto.")]
    public RoomBuilder defaultRoomPrefab;             // ← NUEVO (para 1B: tu 24x24)
    [Tooltip("Si lo asignas, la sala inicial usará este RoomBuilder (si no, usa el defaultRoomPrefab).")]
    public RoomBuilder overrideStartRoomPrefab;       // opcional
}
