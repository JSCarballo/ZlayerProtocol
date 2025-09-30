using UnityEngine;

/// <summary>
/// Represents a single room in the dungeon. At runtime, you can assign
/// enemies, items or props in the room's bounds. The gridPosition is used by
/// DungeonGenerator to keep track of layout.
/// </summary>
public class Room : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPosition;
    // Additional properties such as doors, enemy spawners, etc. can be added.
}