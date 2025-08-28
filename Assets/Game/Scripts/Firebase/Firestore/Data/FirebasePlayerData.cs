#if !UNITY_SERVER
using System.Collections.Generic;
using Statistics;
using UnityEngine;
using UnityEngine.Events;
#endif
public class FirebasePlayerData
{
    public string ID;
    public PlayerRankingData PlayerRanking;
    public Sprite Icon;
    public string Name;
    public List<string> HistoryMatchIDs;
    public List<string> FriendIds;
    public FirebasePlayerData(string id){
        ID = id;
        PlayerRanking = new PlayerRankingData();
    }

    public FirebasePlayerData(string id, string name, PlayerRankingData playerRanking, Sprite icon, List<string> historyMatchIDs, List<string> friendIds)
    {
        ID = id;
        Name = name;
        PlayerRanking =  playerRanking;
        Icon = icon;
        HistoryMatchIDs = historyMatchIDs;
        FriendIds = friendIds;
    }
}