
using System.Threading.Tasks;
using UnityEngine;

public class FirebasePlayerData
{
    public string ID;
    public string Name;
    public int Elo;
    public Sprite Icon;
    public string Email;

    public FirebasePlayerData(string id, string name, int elo, Sprite icon, string email)
    {
        ID = id;
        Name = name;
        Elo = elo;
        Icon = icon;
        Email = email;
    }

    public static async Task<FirebasePlayerData> CreateFirebasePlayerData(string id, System.Uri url, string email)
    {
        var sprite = await GlobalTools.LoadSprite(url);
        return new FirebasePlayerData(id, "BUGAGAGA", 500, sprite, email);
    }

    // public FirebasePlayerData() { } : INetworkSerializable
    // public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    // {
    //     serializer.SerializeValue(ref ID);
    //     serializer.SerializeValue(ref Name);
    //     serializer.SerializeValue(ref Elo);
    // }
}