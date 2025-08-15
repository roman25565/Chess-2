using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    
    [SerializeField] private TextMeshProUGUI nameAndEloText;
    [SerializeField] private Image iconImage;
#if !UNITY_SERVER
    public void SetTime(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        timeText.text = string.Format("{0:D2}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
    }
    
    public void SetPlayerUI(FirebasePlayerData playerData)
    {
        nameAndEloText.text = $"{playerData.Name} + ({playerData.Elo})";
        iconImage.sprite = playerData.Icon;
    }
#endif
}