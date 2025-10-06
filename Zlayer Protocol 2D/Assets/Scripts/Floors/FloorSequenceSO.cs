// Assets/Scripts/Floors/FloorSequenceSO.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ZLayer/Floor Sequence", fileName = "FloorSequence")]
public class FloorSequenceSO : ScriptableObject
{
    public List<FloorDefinitionSO> floors = new(); // orden: 1, 1B, 2, 3...
}
