using Firebase.Firestore;

namespace Statistics
{
[FirestoreData]
public class PlayerRankingData
{
    [FirestoreProperty] public int Elo { get; set; } = 0;
    [FirestoreProperty] public int Position { get; set; } = -1;
}
}