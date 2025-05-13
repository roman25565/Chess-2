#if !UNITY_SERVER
using System.Collections.Generic;
using Statistics;
using UnityEngine;
#endif
public class FirebasePlayerData
{
    public string ID;
    public FirebasePlayerData(string id){
        ID = id;
    }
#if !UNITY_SERVER
    public int Elo;
    public string Email;
    public List<string> HistoryMatchIDs;
    public List<HistoryMatchData> HistoryMatches;
    public Sprite Icon;
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
#endif
}