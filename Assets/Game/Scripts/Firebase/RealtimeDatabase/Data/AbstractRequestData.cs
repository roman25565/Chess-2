using System;
using System.Collections.Generic;
using Firebase.Database;
using UnityEngine;

namespace Firebase.RealtimeDatabase.Data
{
public enum Type
{
    FriendRequest,
    MatchRequest,
}

public abstract class AbstractRequestData
{
    public readonly string RequestId;
    public readonly Type RequestType;
    public readonly string RecipientId;
    public readonly string SenderName;
    public readonly string SenderId;
    public string Status;
    public readonly long Timestamp;
    public readonly string SenderIdTimestamp;

    public AbstractRequestData(DataSnapshot snapshot, Type requestType)
    {
        RequestId = snapshot.Key;
        RequestType = requestType;
        RecipientId = snapshot.Child("recipientId")?.Value as string;
        SenderId = snapshot.Child("senderId")?.Value as string;
        SenderName = snapshot.Child("senderName")?.Value as string;
        Status = snapshot.Child("status")?.Value as string;
        
        var timestampObj = snapshot.Child("timestamp")?.Value;
        if (timestampObj is long) 
            Timestamp = (long)timestampObj;
        else if (timestampObj is string)
            long.TryParse((string)timestampObj, out Timestamp);
        
        SenderIdTimestamp = $"{SenderId}_{Timestamp}";
    }

    public AbstractRequestData(string recipientId, string senderName, string senderId, Type requestType)
    {
        RequestType = requestType;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RecipientId = recipientId;
        SenderName = senderName;
        SenderId = senderId;
        Status = "pending";
        Timestamp = timestamp;
        SenderIdTimestamp = $"{senderId}_{timestamp}";
    }

    public virtual Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["recipientId"] = RecipientId,
            ["senderName"] = SenderName,
            ["senderId"] = SenderId,
            ["status"] = Status,
            ["timestamp"] = Timestamp,
            ["senderId_timestamp"] = SenderIdTimestamp
        };
    }
}
}