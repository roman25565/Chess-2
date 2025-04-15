using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Setting;
using Statistics;
using Zenject;

namespace UI
{

public class PlayerProfileController : MonoBehaviour
{
    [Inject] private Global _global;
    private UIDocument uiDocument;

    private VisualElement root;
    private Button closeButton;
        
    // Інформація про гравця
    private Label playerNameLabel;
    private Label currentEloLabel;
        
    // Статистика
    private Label matchesLabel;
    private Label winsLabel;
    private Label lossesLabel;
    private Label winRateLabel;
        
    // Графік
    private VisualElement graphLines;
    private VisualElement graphDots;
    private Label minEloLabel;
    private Label maxEloLabel;

    private void OnEnable()
    {
        
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        Debug.Log("root" + root);
        if (root == null)
        {
            Debug.LogError("rootVisualElement is null!");
            return;
        }

        InitializeElements();
        UpdateProfile(_global.FirestoreManager.PlayerData);
    }

    private void InitializeElements()
    {
        closeButton = root.Q<Button>("close-button");
        if (closeButton != null)
        {
            closeButton.clicked += HideProfile;
        }
        else
        {
            Debug.LogWarning("Close button not found!");
        }

        // Інформація про гравця
        playerNameLabel = root.Q<Label>("player-name");
        currentEloLabel = root.Q<Label>("current-elo");

        // Статистика
        matchesLabel = root.Q<Label>(className: "stat-value", name: "matches");
        matchesLabel.text = "0";
        winsLabel = root.Q<Label>(className: "stat-value", name: "wins");
        lossesLabel = root.Q<Label>(className: "stat-value", name: "losses");
        winRateLabel = root.Q<Label>(className: "stat-value", name: "win-rate");

        // Графік
        graphLines = root.Q<VisualElement>("graph-lines");
        graphDots = root.Q<VisualElement>("graph-dots");
        minEloLabel = root.Q<Label>(className: "y-axis-label", name: "min");
        maxEloLabel = root.Q<Label>(className: "y-axis-label", name: "max");
    }

    private void UpdateProfile(FirebasePlayerData player)
    {
        playerNameLabel.text = player.Name;
        currentEloLabel.text = player.Elo.ToString();

        // Оновлюємо статистику
        var stats = player.Statistic;
        matchesLabel.text = stats.TotalMatchesPlayed.ToString();
        winsLabel.text = stats.Wins.ToString();
        lossesLabel.text = stats.Losses.ToString();
        winRateLabel.text = $"{stats.WinRate:F1}%";
        
        DrawEloGraph(player.HistoryMatches);
    }

    private void DrawEloGraph(List<HistoryMatchData> matches)
    {
        if (graphLines == null || graphDots == null) return;

        graphLines.Clear();
        graphDots.Clear();

        if (matches == null || matches.Count == 0) return;

        // Знаходимо мінімальний і максимальний рейтинг
        int minElo = int.MaxValue, maxElo = int.MinValue;
        foreach (var match in matches)
        {
            minElo = Mathf.Min(minElo, match.Player1Elo, match.Player2Elo);
            maxElo = Mathf.Max(maxElo, match.Player1Elo, match.Player2Elo);
        }

        int eloRange = Mathf.Max(100, maxElo - minElo);

        // Розміри області графіка
        float graphWidth = graphLines.layout.width;
        float graphHeight = graphLines.layout.height;
        float xStep = graphWidth / (matches.Count - 1);

        // Малюємо лінії рейтингу
        Vector2 prevPoint = Vector2.zero;
        for (int i = 0; i < matches.Count; i++)
        {
            float xPos = i * xStep;
            float yPos = graphHeight - ((matches[i].Player1Elo - minElo) / (float)eloRange * graphHeight);

            var point = new Vector2(xPos, yPos);

            // Точка
            var dot = new VisualElement();
            dot.AddToClassList("graph-dot");
            dot.style.left = point.x - 5;
            dot.style.top = point.y - 5;
            graphDots.Add(dot);

            // Лінія (крім першої точки)
            if (i > 0)
            {
                var line = new VisualElement();
                line.AddToClassList("graph-line");
                line.style.left = prevPoint.x;
                line.style.top = prevPoint.y;
                line.style.width = Vector2.Distance(prevPoint, point);
                line.style.rotate = new StyleRotate(new Rotate(new Angle(
                    Mathf.Atan2(point.y - prevPoint.y, point.x - prevPoint.x) * Mathf.Rad2Deg
                )));
                graphLines.Add(line);
            }

            prevPoint = point;
        }
        
        minEloLabel.text = minElo.ToString();
        maxEloLabel.text = maxElo.ToString();
    }

    public void HideProfile()
    {
        root.style.display = DisplayStyle.None;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.clicked -= HideProfile;
        }
    }
}

}