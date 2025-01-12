using UnityEngine;

[CreateAssetMenu(fileName = "CellStates", menuName = "Settings/CellStates")]
public class CellStates : ScriptableObject
{
    public CellStatesData attacked;
    public CellStatesData moved;
    public CellStatesData selected;
}

[System.Serializable]
public class CellStatesData
{
    public Sprite Value;
    public float Alpha;
}