#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
public class FriendRequestData : AbstractRequestData

{
    public static readonly string CollectionName = "FriendRequests";
    public FriendRequestData(DataSnapshot snapshot) : base(snapshot, Type.FriendRequest)
    {
    }

    public FriendRequestData(string recipientId, string senderName, string senderId) : base(recipientId, senderName, senderId, Type.FriendRequest)
    {
    }

}

public class RemoveFriendRequestData : AbstractRequestData
{
    public static readonly string CollectionName = "RemoveFriendRequests";
    public RemoveFriendRequestData(DataSnapshot snapshot) : base(snapshot, Type.RemoveFriendRequest)
    {
    }

    public RemoveFriendRequestData(string recipientId, string senderName, string senderId) : base(recipientId, senderName, senderId, Type.RemoveFriendRequest)
    {
    }

}
}
#endif