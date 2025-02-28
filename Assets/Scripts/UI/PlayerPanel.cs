using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    
    [SerializeField] private TextMeshProUGUI NameText;
    [SerializeField] private TextMeshProUGUI EloText;
    [SerializeField] private Image IconImage;
    public void SetTime(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        timeText.text = string.Format("{0:D2}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
    }
    
    public void SetPlayerUI(FirebasePlayerData playerData)
    {
        NameText.text = playerData.Name;
        EloText.text = playerData.Elo.ToString();
        IconImage.sprite = playerData.Icon;
    }

    public void EndGame(int newElo)
    {
        EloText.text = newElo.ToString();
    }
}