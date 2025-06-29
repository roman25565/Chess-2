#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
public class ReConnectRequestData
{
    public const string RelayJoinCodeKey = "RelayJoinCode";
    
    public static readonly string CollectionName = "ReConnects";
    
    public readonly string RecipientId;
    public readonly long Timestamp;
    public readonly string RelayJoinCode;

    public ReConnectRequestData(string recipientId, string relayJoinCode)
    {
        RelayJoinCode  = relayJoinCode;
        RecipientId = recipientId;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    public ReConnectRequestData(DataSnapshot snapshot)
    {
        RecipientId = snapshot.Child(AbstractRequestData.RecipientIdKey)?.Value as string;
        RelayJoinCode = snapshot.Child(RelayJoinCodeKey)?.Value as string;
            
        var timestampObj = snapshot.Child(AbstractRequestData.TimestampKey)?.Value;
        if (timestampObj is long)
            Timestamp = (long)timestampObj;
        else if (timestampObj is string)
            long.TryParse((string)timestampObj, out Timestamp);
    }

    public virtual Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [AbstractRequestData.RecipientIdKey] = RecipientId,
            [RelayJoinCodeKey] = RelayJoinCode,
            [AbstractRequestData.TimestampKey] = Timestamp
        };
    }
}
}
#endif