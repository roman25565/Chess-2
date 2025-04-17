using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
[System.Serializable]
public class FriendRequestData
{
    public string senderId;
    public string senderName;
    public string status;
    public long timestamp;
    public string senderIdTimestamp;

    // Конструктор з DataSnapshot
    public FriendRequestData(DataSnapshot snapshot)
    {
        senderId = snapshot.Child("senderId")?.Value as string;
        senderName = snapshot.Child("senderName")?.Value as string;
        status = snapshot.Child("status")?.Value as string;
        
        var timestampObj = snapshot.Child("timestamp")?.Value;
        if (timestampObj is long) 
            timestamp = (long)timestampObj;
        else if (timestampObj is string)
            long.TryParse((string)timestampObj, out timestamp);
        
        senderIdTimestamp = $"{senderId}_{timestamp}";
    }

    // Конструктор для створення нового запиту
    public FriendRequestData(string senderId, string senderName)
    {
        this.senderId = senderId;
        this.senderName = senderName;
        this.status = "pending";
        this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.senderIdTimestamp = $"{senderId}_{timestamp}";
    }

    // Перетворення в Dictionary для Firebase
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["senderId"] = senderId,
            ["senderName"] = senderName,
            ["status"] = status,
            ["timestamp"] = timestamp,
            ["senderId_timestamp"] = senderIdTimestamp
        };
    }
}
}