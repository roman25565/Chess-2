#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
public class ReConnectRequestData
{
    public const string IpKey = "IPAdress";
    private const string PortKey = "Port";
    
    public static readonly string CollectionName = "ReConnects";
    
    public readonly string RecipientId;
    public readonly long Timestamp;
    public readonly string IP;
    public readonly ushort Port;

    public ReConnectRequestData(string recipientId, string ip,ushort port)
    {
        RecipientId = recipientId;
        IP = ip;
        Port = port;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    public ReConnectRequestData(DataSnapshot snapshot)
    {
        RecipientId = snapshot.Child(AbstractRequestData.RecipientIdKey)?.Value as string;
        IP = snapshot.Child(IpKey)?.Value as string;
            
        var portObj = snapshot.Child(PortKey)?.Value;
        if (portObj != null)
        {
            ushort.TryParse(portObj.ToString(), out ushort port);
            Port = port;
        }
            
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
            [IpKey] = IP,
            [PortKey] = Port,
            [AbstractRequestData.TimestampKey] = Timestamp
        };
    }
}
}
#endif