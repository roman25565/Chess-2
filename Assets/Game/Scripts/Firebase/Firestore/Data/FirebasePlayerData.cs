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
    public List<string> HistoryMatchIDs;
    public Sprite Icon;
    public string Name;
    public List<string> FriendIds;

    public FirebasePlayerData(string id, string name, int elo, Sprite icon, List<string> historyMatchIDs, List<string> friendIds)
    {
        ID = id;
        Name = name;
        Elo = elo;
        Icon = icon;
        HistoryMatchIDs = historyMatchIDs;
        FriendIds = friendIds;
    }
}