using System;
using System.Collections.Generic;
using Firebase.Database;

namespace Firebase.RealtimeDatabase.Data
{
public class FriendRequestData : AbstractRequestData

{
    public FriendRequestData(DataSnapshot snapshot) : base(snapshot, Type.FriendRequest)
    {
    }

    public FriendRequestData(string recipientId, string senderName, string senderId) : base(recipientId, senderName, senderId, Type.FriendRequest)
    {
    }

}
}