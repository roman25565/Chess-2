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

    public FirebasePlayerData(string id, string name, int elo, Sprite icon, string email, List<string> historyMatchIDs)
    {
        ID = id;
        Name = name;
        Elo = elo;
        Icon = icon;
        Email = email;
        HistoryMatchIDs = historyMatchIDs;
        Statistic = new PlayerStatistic();
    }

    public void SetHistoryMatches(List<HistoryMatchData> matches)
    {
        HistoryMatches = matches;
    }

    // public FirebasePlayerData() { } : INetworkSerializable
    // public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    // {
    //     serializer.SerializeValue(ref ID);
    //     serializer.SerializeValue(ref Name);
    //     serializer.SerializeValue(ref Elo);
    // }
}