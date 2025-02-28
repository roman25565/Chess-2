using System.Collections.Generic;
using UnityEngine;

public class FirebasePlayerData
{
    public int Elo;
    public string Email;
    public List<string> HistoryIDs;
    public Sprite Icon;
    public string ID;
    public string Name;

    public FirebasePlayerData(string id, string name, int elo, Sprite icon, string email, List<string> historyIDs)
    {
        ID = id;
        Name = name;
        Elo = elo;
        Icon = icon;
        Email = email;
        HistoryIDs = historyIDs;
    }

    // public FirebasePlayerData() { } : INetworkSerializable
    // public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    // {
    //     serializer.SerializeValue(ref ID);
    //     serializer.SerializeValue(ref Name);
    //     serializer.SerializeValue(ref Elo);
    // }
}