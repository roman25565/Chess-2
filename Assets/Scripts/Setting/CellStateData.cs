using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CellStates", menuName = "Settings/CellStates")]
public class CellStates : ScriptableObject
{
    public CellStatesData attacked;
    public CellStatesData moved;
    public CellStatesData selected;
    public CellStatesData none;
}

[Serializable]
public class CellStatesData
{
    [FormerlySerializedAs("Value")] public Sprite value;
    public Color color = Color.white;
}