#if !UNITY_SERVER
using System.Collections.Generic;
using Statistics;
using UnityEngine;
using UnityEngine.Events;
#endif
public class FirebasePlayerData
{
    public string ID;
    public FirebasePlayerData(string id){
        ID = id;
    }
    public int Elo;
    public string Email;
    public List<string> HistoryMatchIDs;
    public Sprite Icon;
    public string Name;
    public PlayerStatistic Statistic;
    public List<string> FriendIds;
    
    public List<HistoryMatchData> HistoryMatches;
    public bool HistoryMatchesLoading;

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
}