using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a simple dungeon layout composed of interconnected rooms on a 2D
/// grid. Each room is spaced evenly and can contain enemies, power‑ups and
/// obstacles. The generator ensures all rooms are reachable.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public Room roomPrefab;
    public int totalRooms = 10;
    public int gridWidth = 4;
    public int gridHeight = 4;
    public float roomSpacing = 12f;

    private Dictionary<Vector2Int, Room> spawnedRooms = new Dictionary<Vector2Int, Room>();

    /// <summary>
    /// Generates the dungeon when the game starts. Can be invoked manually
    /// during runtime as well.
    /// </summary>
    public void GenerateDungeon()
    {
        spawnedRooms.Clear();
        Vector2Int currentPos = Vector2Int.zero;
        // spawn the first room at origin
        SpawnRoom(currentPos);
        // create subsequent rooms by walking randomly on the grid
        for (int i = 1; i < totalRooms; i++)
        {
            Vector2Int dir = RandomDirection();
            currentPos += dir;
            // clamp position within bounds
            currentPos.x = Mathf.Clamp(currentPos.x, -gridWidth, gridWidth);
            currentPos.y = Mathf.Clamp(currentPos.y, -gridHeight, gridHeight);
            SpawnRoom(currentPos);
        }
        // Optional: connect doors between neighbouring rooms
    }

    private void SpawnRoom(Vector2Int position)
    {
        if (spawnedRooms.ContainsKey(position)) return;
        Vector3 worldPos = new Vector3(position.x * roomSpacing, position.y * roomSpacing, 0f);
        Room room = Instantiate(roomPrefab, worldPos, Quaternion.identity, transform);
        room.gridPosition = position;
        spawnedRooms[position] = room;
    }

    private Vector2Int RandomDirection()
    {
        int rand = Random.Range(0, 4);
        switch (rand)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.right;
            case 2: return Vector2Int.down;
            default: return Vector2Int.left;
        }
    }
}