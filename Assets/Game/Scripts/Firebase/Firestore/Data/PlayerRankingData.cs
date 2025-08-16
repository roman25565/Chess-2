using Firebase.Firestore;

namespace Statistics
{
[FirestoreData]
public class PlayerRankingData
{
    [FirestoreProperty] public int Elo { get; set; }
    [FirestoreProperty] public int Position { get; set; }
}
}