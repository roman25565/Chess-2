using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Google;
using UnityEngine.Networking;

public class FirestoreManager
{
    private const string IDKey = "ID";
    private const string NameKey = "Name";
    private const string EloKey = "Elo";
    private const string IconURLKey = "IconURL";
    private const string EmailKey = "Email";
    
    private FirebaseFirestore db;
    public FirebasePlayerData PlayerData;

    #region PlayersData
    private const string PlayersDataCollectionName = "Players";

    public async Task Init()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
#if UNITY_EDITOR
                db.Settings.PersistenceEnabled = false;
#endif
                Debug.Log("Firebase initialized successfully.");
                // Call the method to add or fetch player data
                // AddOrFetchPlayerData("player123", "JohnDoe", 1200);
                
                // var id = "004";
                // var playerData = await GetPlayerData(id);
                // Debug.Log(playerData?.Elo);
                // if (playerData == null)
                // {
                //     SingUp(FirebasePlayerData.CreateFirebasePlayerData(id));
                // }
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus.ToString());
            }
        }
        catch (Exception e)
        {
            Debug.LogError("error" + e);
            Console.WriteLine(e);
            throw;
        }
    }

    public delegate void GetPlayerDataCallBack(FirebasePlayerData result);
    public async Task<FirebasePlayerData> GetPlayerData(string playerId,GetPlayerDataCallBack callback)
    {
        FirebasePlayerData result = null;
        try
        {
            var docRef = db.Collection(PlayersDataCollectionName).Document(playerId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var existingName = snapshot.GetValue<string>(NameKey);
                var existingElo = snapshot.GetValue<int>(EloKey);
                var imageURL = snapshot.GetValue<string>(IconURLKey);
                var email = snapshot.GetValue<string>(EmailKey);
                Debug.Log("Load From DB");
                var ico = await GlobalTools.LoadSprite(new Uri(imageURL));
                result = new FirebasePlayerData(playerId, existingName, existingElo, ico, email);
            }
            else
            {
                result = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch document for playerId '{playerId}': {ex.Message}");
        }

        callback(result);
        return result;
    }

    public void SingUp(string testId)
    {
        Dictionary<string, object> player = new Dictionary<string, object>
        {
            { IDKey, testId },
            { NameKey, "BUGAGAGA" },
            { EloKey, 500 },
            { IconURLKey, "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no" },
            { EmailKey, "test@gmail.com" },
        };

        SingUp(player, testId);
    }

    public void SingUp(GoogleSignInUser user)
    {
        Dictionary<string, object> player = new Dictionary<string, object>
        {
            { IDKey, user.UserId },
            { NameKey, user.DisplayName },
            { EloKey, 500 },
            { IconURLKey, user.ImageUrl.ToString() },
            { EmailKey, user.Email },
        };
        SingUp(player, user.UserId);
    }

    private void SingUp(Dictionary<string, object> playerData, string playerId)
    {
        DocumentReference docRef = db.Collection(PlayersDataCollectionName).Document(playerId);
        docRef.SetAsync(playerData).ContinueWithOnMainThread(async setTask =>
        {
            if (setTask.IsFaulted)
            {
                Debug.LogError("Failed to add player: " + setTask.Exception);
            }
            else
            {
                Debug.Log("Player added successfully.");
                var icon = await GlobalTools.LoadSprite(new Uri(playerData[IconURLKey].ToString()));
                PlayerData = new FirebasePlayerData
                (
                    id: playerData[IDKey].ToString(), 
                    name: playerData[NameKey].ToString(),
                    elo: int.Parse(playerData[EloKey].ToString()),
                    icon: icon,
                    email: playerData[EmailKey].ToString()
                );
            }
        });
    }

    public void BdSetElo(string playerId, int newElo)
    {
        DocumentReference docRef = db.Collection(PlayersDataCollectionName).Document(playerId);
        
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { EloKey, newElo }
        };
        
        docRef.UpdateAsync(updates).ContinueWith(task => {
            if (task.IsCompleted) {
                Debug.Log("Document updated successfully.");
            } else if (task.IsFaulted) {
                Debug.LogError("Error updating document: " + task.Exception);
            }
        });
    }
    
    #endregion
    #region MatchData

    private const string MatchesDataCollectionName = "Matches";
    
    private const string WinnerID = "WinnerID";
    private const string Date = "Date";
    private const string Player1ID = "Player1ID";
    private const string Player2ID = "Player2ID";
    private const string ArrangementList1 = "ArrangementList1";
    private const string ArrangementList2 = "ArrangementList2";
    private const string MoveHistory = "MoveHistory";
    
    private const string Row = "Row";
    private const string Column = "Column";
    private const string PieceType = "PieceType";

    public async Task SaveMatchHistory(string winnerID,
        string player1ID, ArrangementEntry[] arrangement1,
        string player2ID, ArrangementEntry[] arrangement2,
        List<Move> moveHistory)
    {
        var collectionRef = db.Collection(MatchesDataCollectionName);

        var arrangementList1 = ConvertArrangement(arrangement1);
        var arrangementList2 = ConvertArrangement(arrangement2);
        var history = ConvertMoveList(moveHistory);
        var matchData = new Dictionary<string, object>
        {
            { WinnerID, winnerID },
            { Date, DateTime.UtcNow },
            { Player1ID, player1ID },
            { ArrangementList1, arrangementList1 },
            { Player2ID, player2ID },
            { ArrangementList2, arrangementList2 },
            { MoveHistory, history }
        };
        Debug.Log(matchData);
        await collectionRef.AddAsync(matchData);
        Debug.Log("Match saved");
    }

    private List<Dictionary<string, object>> ConvertMoveList(List<Move> moveHistory)
    {
        var result = new List<Dictionary<string, object>>();

        foreach (var move in moveHistory)
        {
            var dict = new Dictionary<string, object>
            {
                { "From", new Dictionary<string, object>
                    {
                        { Row, move.From.Row },
                        { Column, move.From.Column }
                    }
                },
                { "To", new Dictionary<string, object>
                    {
                        { Row, move.To.Row },
                        { Column, move.To.Column }
                    }
                }
            };
            result.Add(dict);
        }

        return result;
    }

    private Dictionary<string, object> ConvertArrangement(ArrangementEntry[] arrangement)
    {
        var result = new Dictionary<string, object>();

        for (int i = 0; i < arrangement.Length; i++)
        {
            var entry = arrangement[i];
            var dict = new Dictionary<string, object>
            {
                { Row, entry.row },
                { Column, entry.column },
                { PieceType, entry.pieceType.ToString() }
            };

            // Add the entry to the result dictionary with a key like "0", "1", "2", etc.
            result[i.ToString()] = dict;
        }

        return result;
    }

    #endregion
}