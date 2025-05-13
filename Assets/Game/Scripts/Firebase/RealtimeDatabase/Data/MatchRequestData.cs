#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
public class MatchRequestData : AbstractRequestData

{
    private const string IpKey = "IP";
    private const string PortKey = "Port";
    
    public static readonly string CollectionName = "MatchRequests";
    public string Ip { get; private set; }
    public ushort Port { get; private set; }
    public MatchRequestData(DataSnapshot snapshot) : base(snapshot, Type.MatchRequest)
    {
        Ip = snapshot.Child(IpKey)?.Value as string;
        
        var portObj = snapshot.Child(PortKey)?.Value;
        if (portObj != null)
        {
            ushort tempPort;
            if (ushort.TryParse(portObj.ToString(), out tempPort))
            {
                Port = tempPort;
            }
        }
    }

    public MatchRequestData(string recipientId, string senderName, string senderId) 
        : base(recipientId, senderName, senderId, Type.MatchRequest)
    {
        Ip = string.Empty;
        Port = 0;
    }

    public void SetConnectionInfo(string ip, ushort port)
    {
        Ip = ip;
        Port = port;
    }

    public override Dictionary<string, object> ToDictionary()
    {
        var dict = base.ToDictionary();
        
        if (!string.IsNullOrEmpty(Ip))
            dict[IpKey] = Ip;
        
        if (Port > 0)
            dict[PortKey] = Port;
        
        return dict;
    }

}
}
#endif