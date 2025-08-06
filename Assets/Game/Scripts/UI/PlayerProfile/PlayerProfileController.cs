using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if !UNITY_SERVER
using System.Collections.Generic;
using Setting;
using Statistics;
using Zenject;
#endif
namespace UI
{

public class PlayerProfileController : MonoBehaviour
{
    [Inject] private Global _global;
    
    [SerializeField] private HistoryPanel historyPanel;
    [SerializeField] private FriendsPanel friendsPanel;
    
    [SerializeField] private TextMeshProUGUI topProfileText;
    [SerializeField] private TextMeshProUGUI statisticProfileText;
    
    [SerializeField] private Image playerImage;
    private string _targetPlayerId;

    public void SetTargetId(string id)
    {
        _targetPlayerId = id;
        historyPanel.SetTargetId(id);
        friendsPanel.SetTargetId(id);
    }
    
    public void OnEnable()
    {
#if UNITY_EDITOR
        if (_global.FirestoreManager == null) return;
#endif
        if (_targetPlayerId == null) return;
        _global.FirestoreManager.LoadHistory(_targetPlayerId, UpdateEloGraph);
        _global.FirestoreManager.LoadPlayerData(_targetPlayerId, UpdateTopProfile);
        _global.FirestoreManager.LoadStatistic(_targetPlayerId, UpdateStatisticProfile);
        historyPanel.ReloadUI();
        friendsPanel.ReloadUI();
    }

    private void OnDisable()
    {
        _targetPlayerId = null;
    }

    private void UpdateTopProfile(string id,FirebasePlayerData player)
    {
        if (id != _targetPlayerId) return;
        playerImage.color = Color.white;
        playerImage.sprite = player.Icon;
        topProfileText.text = $"{player.Name} ({player.Elo})";
    }

    private void UpdateStatisticProfile(string id, PlayerStatistic statistic)
    {
        if (id != _targetPlayerId) return;

        StringBuilder sb = new StringBuilder();

        // General Information
        sb.AppendLine($"<size=25><b>General Statistics</b></size>");
        sb.AppendLine(
            $"ELO Rating: <b>{statistic.CurrentEloRating}</b> (Peak: {statistic.PeakEloRating}, Lowest: {statistic.LowestEloRating})");
        sb.AppendLine($"Matches Played: <b>{statistic.TotalMatchesPlayed}</b>");
        sb.AppendLine(
            $"Wins: <b>{statistic.Wins}</b> | Losses: <b>{statistic.Losses}</b> | Draws: <b>{statistic.Draws}</b>");
        sb.AppendLine($"Win Rate: <b>{statistic.WinRate:F1}%</b>");
        sb.AppendLine();

        // Color Performance
        sb.AppendLine($"<size=25><b>By Piece Color</b></size>");
        sb.AppendLine(
            $"Wins as White: <b>{statistic.WinsAsWhite}</b> | Losses as White: <b>{statistic.LossesAsWhite}</b>");
        sb.AppendLine(
            $"Wins as Black: <b>{statistic.WinsAsBlack}</b> | Losses as Black: <b>{statistic.LossesAsBlack}</b>");
        sb.AppendLine();

        // Time Statistics
        sb.AppendLine($"<size=25><b>Time Played</b></size>");
        sb.AppendLine($"Total Play Time: <b>{FormatTimeSpan(statistic.TotalPlayTime)}</b>");
        sb.AppendLine(
            $"Average Game Duration: <b>{FormatTimeSpan(TimeSpan.FromSeconds(statistic.TotalPlayTimeHours / Math.Max(1, statistic.TotalMatchesPlayed)))}</b>");
        sb.AppendLine();

        // Play Style
        sb.AppendLine($"<size=25><b>Play Style</b></size>");
        sb.AppendLine(
            $"Checkmates Given: <b>{statistic.KingsDefeated}</b> | Received: <b>{statistic.KingsLost}</b>");
        sb.AppendLine($"Resignations: <b>{statistic.Resignations}</b> | Timeouts: <b>{statistic.Timeouts}</b>");
        sb.AppendLine(
            $"Draws: <b>{statistic.DrawsByAgreement}</b>");
        sb.AppendLine();

        // Streaks
        sb.AppendLine($"<size=25><b>Performance Streaks</b></size>");
        sb.AppendLine($"Current Win Streak: <b>{statistic.CurrentWinStreak}</b> (Max: {statistic.MaxWinStreak})");
        sb.AppendLine($"Current Losing Streak: <b>{statistic.CurrentLoseStreak}</b> (Max: {statistic.MaxLoseStreak})");
        sb.AppendLine();

        // Activity Dates
        sb.AppendLine($"<size=25><b>Activity</b></size>");
        sb.AppendLine($"Registration Date: <b>{statistic.GetRegistrationDate().ToString("MMM dd, yyyy", CultureInfo.InvariantCulture)}</b>");
        sb.AppendLine($"Last Played: <b>{statistic.GetLastPlayedDate().ToString("MMM dd, yyyy HH:mm", CultureInfo.InvariantCulture)}</b>");

        statisticProfileText.text = sb.ToString();
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        return $"{timeSpan.Seconds}s";
    }

    private void UpdateEloGraph(string id,List<HistoryMatchData> historyMatches)
    {
        foreach (var historyMatchData in historyMatches)
        {
            if (historyMatchData == null) continue;
        }
    }
}

}