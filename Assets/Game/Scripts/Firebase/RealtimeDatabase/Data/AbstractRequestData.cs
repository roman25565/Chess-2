#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Firebase.Database;
using UnityEngine;

namespace Firebase.RealtimeDatabase.Data
{
public enum Type
{
    FriendRequest,
    RemoveFriendRequest,
    MatchRequest,
}

public class StatusKeys
{
    public const string PendingKey = "Pending";
    public const string AcceptedKey = "Accepted";
    public const string RejectedKey = "Rejected";
}

public abstract class AbstractRequestData
{
    public static readonly string RequestIdKey = "RequestId"; 
    public static readonly string RecipientIdKey = "RecipientId"; 
    public static readonly string SenderIdKey = "SenderId"; 
    public static readonly string SenderNameKey = "SenderName"; 
    public static readonly string StatusKey = "Status";
    public static readonly string TimestampKey = "Timestamp"; 
    public static readonly string SenderIdTimestampKey = "SenderId_Timestamp"; 
    
    public static readonly StatusKeys StatusKeys = new();
    
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
        RecipientId = snapshot.Child(RecipientIdKey)?.Value as string;
        SenderId = snapshot.Child(SenderIdKey)?.Value as string;
        SenderName = snapshot.Child(SenderNameKey)?.Value as string;
        Status = snapshot.Child(StatusKey)?.Value as string;
        
        var timestampObj = snapshot.Child(TimestampKey)?.Value;
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
        Status = StatusKeys.PendingKey;
        Timestamp = timestamp;
        SenderIdTimestamp = $"{senderId}_{timestamp}";
    }

    public virtual Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [RecipientIdKey] = RecipientId,
            [SenderNameKey] = SenderName,
            [SenderIdKey] = SenderId,
            [StatusKey] = Status,
            [TimestampKey] = Timestamp,
            [SenderIdTimestampKey] = SenderIdTimestamp
        };
    }
}
}
#endif