using System;
using TMPro;
using UnityEngine;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    public void SetTime(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        textMesh.text = string.Format("{0:D2}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
    }
}