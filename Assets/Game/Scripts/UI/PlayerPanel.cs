using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    
    [SerializeField] private TextMeshProUGUI nameAndEloText;
    [SerializeField] private Image iconImage;
#if !UNITY_SERVER
    public void SetTime(float time)
    {
        if (time == 1000)
        {
            timeText.text = "--:--";
            return;
        }
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        timeText.text = string.Format("{0:D2}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
    }
    
    public void SetPlayerUI(FirebasePlayerData playerData)
    {
        var playerRankingPosition = playerData.PlayerRanking.Position;
        
        nameAndEloText.text = $"{playerData.Name} ({playerData.PlayerRanking.Elo})";
        if (playerRankingPosition != -1) nameAndEloText.text += $" #{playerRankingPosition}";
        iconImage.sprite = playerData.Icon;
    }
#endif
}