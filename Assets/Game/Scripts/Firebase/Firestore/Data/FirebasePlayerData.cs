using System.Collections.Generic;
using Statistics;
using UnityEngine;

public class FirebasePlayerData
{
    public int Elo;
    public string Email;
    public List<string> HistoryMatchIDs;
    public List<HistoryMatchData> HistoryMatches;
    public Sprite Icon;
    public string ID;
    public string Name;
    public PlayerStatistic Statistic;
    public List<string> FriendIds;

    public FirebasePlayerData(string id, string name, int elo, Sprite icon, 
        string email, List<string> historyMatchIDs, List<string> friendIds)
    {
        ID = id;
        Name = name;
        Elo = elo;
        Icon = icon;
        Email = email;
        HistoryMatchIDs = historyMatchIDs;
        Statistic = new PlayerStatistic();
        FriendIds = friendIds;
    }

    public void SetHistoryMatches(List<HistoryMatchData> matches)
    {
        HistoryMatches = matches;
    }
}