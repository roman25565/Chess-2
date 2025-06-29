#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
public class MatchRequestData : AbstractRequestData

{
    public const string RelayJoinCodeKey = "RelayJoinCode";
    
    public static readonly string CollectionName = "MatchRequests";
    public string RelayJoinCode;
    public MatchRequestData(DataSnapshot snapshot) : base(snapshot, Type.MatchRequest)
    {
        RelayJoinCode = snapshot.Child(RelayJoinCodeKey)?.Value as string;
    }

    public MatchRequestData(string recipientId, string senderName, string senderId) 
        : base(recipientId, senderName, senderId, Type.MatchRequest)
    {
        RelayJoinCode = string.Empty;
    }

    public void SetConnectionInfo(string relayJoinCode)
    {
        RelayJoinCode = relayJoinCode;
    }

    public override Dictionary<string, object> ToDictionary()
    {
        var dict = base.ToDictionary();
        
        if (!string.IsNullOrEmpty(RelayJoinCode))
            dict[RelayJoinCodeKey] = RelayJoinCode;
        
        return dict;
    }

}
}
#endif