using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Board;
using Board.Piece;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.RealtimeDatabase;
using Google;
using Unity.Mathematics;
using UnityEngine;

public class FirestoreManager
{
    private FirebaseFirestore _db;
    public FirebasePlayerData PlayerData;
    public FirestoreStatistic Statistic;
    public RealtimeDatabase RealtimeDatabase;
    public async Task Init()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                _db = FirebaseFirestore.DefaultInstance;
#if UNITY_EDITOR
                if (_db.Settings.PersistenceEnabled) _db.Settings.PersistenceEnabled = false;
#endif
                Debug.Log("Firebase initialized successfully.");
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
            
            Statistic = new FirestoreStatistic(_db);
        }
        catch (Exception e)
        {
            Debug.LogError("error" + e);
            Console.WriteLine(e);
            throw;
        }
    }

    #region PlayersData

    private const string PlayersDataCollectionName = "Players";

    private const string IDKey = "ID";
    private const string NameKey = "Name";
    private const string EloKey = "Elo";
    private const string IconURLKey = "IconURL";
    private const string EmailKey = "Email";
    private const string HistoryIDs = "HistoryIDs";

    public delegate void GetPlayerDataCallBack(FirebasePlayerData result);

    public void SetPlayerData(FirebasePlayerData playerData)
    {
        PlayerData = playerData;
        RealtimeDatabase = new RealtimeDatabase(playerData.ID);
    }
    
    public async Task<FirebasePlayerData> GetPlayerData(string playerId, GetPlayerDataCallBack callback)
    {
        FirebasePlayerData result = null;
        try
        {
            var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var existingName = snapshot.GetValue<string>(NameKey);
                var existingElo = snapshot.GetValue<int>(EloKey);
                var imageURL = snapshot.GetValue<string>(IconURLKey);
                var email = snapshot.GetValue<string>(EmailKey);
                var historyIds = snapshot.GetValue<List<string>>(HistoryIDs);

                Debug.Log("Load From DB");
                var ico = await GlobalTools.LoadSprite(new Uri(imageURL));
                result = new FirebasePlayerData(playerId, existingName, existingElo, ico, email, historyIds);
            }

            callback(result);
        }
        catch (Exception e)
        {
            Debug.LogError(playerId + e);
            throw;
        }
        return result;
    }

    public async void GetIcon(string playerId, Action<Sprite> action)
    {
        Debug.Log("Get Icon");
        Sprite result = null;
        try
        {
            var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var imageURL = snapshot.GetValue<string>(IconURLKey);
                result = await GlobalTools.LoadSprite(new Uri(imageURL));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }

        action.Invoke(result);
    }

    private async Task<string> GetPlayerName(string playerId)
    {
        string result = null;
        try
        {
            var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists) result = snapshot.GetValue<string>(NameKey);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch document for playerId '{playerId}': {ex.Message}");
        }

        return result;
    }

    public void SingUp(string testId)
    {
        var player = new Dictionary<string, object>
        {
            { IDKey, testId },
            { NameKey, "BUGAGAGA" },
            { EloKey, 500 },
            {
                IconURLKey,
                "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no"
            },
            { EmailKey, "test@gmail.com" }
        };

        SingUp(player, testId);
    }


    public void SingUp(GoogleSignInUser user)
    {
        var player = new Dictionary<string, object>
        {
            { IDKey, user.UserId },
            { NameKey, user.DisplayName },
            { EloKey, 500 },
            { IconURLKey, user.ImageUrl.ToString() },
            { EmailKey, user.Email },
            {HistoryIDs, new object[]{} }
        };
        SingUp(player, user.UserId);
    }

    private void SingUp(Dictionary<string, object> playerData, string playerId)
    {
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
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
                var firebasePlayerData = new FirebasePlayerData
                (
                    playerData[IDKey].ToString(),
                    playerData[NameKey].ToString(),
                    int.Parse(playerData[EloKey].ToString()),
                    icon,
                    playerData[EmailKey].ToString(),
                    new List<string>()
                );
                SetPlayerData(firebasePlayerData);
            }
        });
    }

    public void BdSetElo(string playerId, int newElo)
    {
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);

        var updates = new Dictionary<string, object>
        {
            { EloKey, newElo }
        };

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Debug.Log("Document updated successfully.");
            else if (task.IsFaulted) Debug.LogError("Error updating document: " + task.Exception);
        });
    }

    private void BdAddHistoryId(string playerId, string historyId)//TODO NOTWORK 
    {
        Debug.Log("playerId: " + playerId);
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
        
        docRef.UpdateAsync(HistoryIDs, FieldValue.ArrayUnion(historyId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("HistoryMatchIDs updated successfully.");
                else if (task.IsFaulted) Debug.LogError("Error updating document: " + task.Exception);
            });
    }

    #endregion

    #region MatchData

    private const string MatchesDataCollectionName = "Matches";

    private const string WinnerID = "WinnerID";
    private const string Date = "Date";
    private const string Player1ID = "Player1ID";
    private const string Player1Elo = "Player1Elo";
    private const string Player2ID = "Player2ID";
    private const string Player2Elo = "Player2Elo";
    private const string ArrangementList1 = "ArrangementList1";
    private const string ArrangementList2 = "ArrangementList2";
    private const string MoveHistory = "MoveHistory";

    private const string Row = "Row";
    private const string Column = "Column";
    private const string PieceType = "PieceType";

    public void SaveMatchHistory(string winnerID,
        string player1ID, int player1Elo, ArrangementEntry[] arrangement1,
        string player2ID, int player2Elo, ArrangementEntry[] arrangement2,
        List<Move> moveHistory)
    {
        var collectionRef = _db.Collection(MatchesDataCollectionName);

        var arrangementList1 = ConvertArrangement(arrangement1);
        var arrangementList2 = ConvertArrangement(arrangement2);
        var history = ConvertMoveList(moveHistory);
        var matchData = new Dictionary<string, object>
        {
            { WinnerID, winnerID },
            { Date, DateTime.UtcNow },
            { Player1ID, player1ID },
            { Player1Elo, player1Elo },
            { ArrangementList1, arrangementList1 },
            { Player2ID, player2ID },
            { Player2Elo, player2Elo },
            { ArrangementList2, arrangementList2 },
            { MoveHistory, history }
        };
        Debug.Log(matchData);
        collectionRef.AddAsync(matchData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                var docRef = task.Result;
                Debug.Log("matchData Added successfully. " + docRef.Id );
                BdAddHistoryId(player1ID, docRef.Id);
                BdAddHistoryId(player2ID, docRef.Id);
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Error updating document: " + task.Exception);
            }
        });
        Debug.Log("Match saved");
    }

    private List<Dictionary<string, object>> ConvertMoveList(List<Move> moveHistory)
    {
        var result = new List<Dictionary<string, object>>();

        foreach (var move in moveHistory)
        {
            var dict = new Dictionary<string, object>
            {
                {
                    "From", new Dictionary<string, object>
                    {
                        { Row, move.From.Row },
                        { Column, move.From.Column }
                    }
                },
                {
                    "To", new Dictionary<string, object>
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

        for (var i = 0; i < arrangement.Length; i++)
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

    public void GetAllHistory(List<string> historyIDs, Action<List<HistoryMatchData>> callback)
    {
        if (historyIDs == null)
            return;
                
        var completedRequests = 0;
        var historyMatches = new List<HistoryMatchData>();
        foreach (var historyID in historyIDs)
        {
            GetHistory(historyID, (matchData) =>
            {
                if (matchData != null)
                {
                    historyMatches.Add(matchData);
                }
        
                completedRequests++;
        
                // Перевіряємо, чи всі запити завершені
                if (completedRequests == historyIDs.Count)
                {
                    callback?.Invoke(historyMatches);
                }
            });
        }
    }

    public void GetHistory(string historyID, Action<HistoryMatchData> action)
    {
        var docRef = _db.Collection(MatchesDataCollectionName).Document(historyID);
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(async task =>
        {
            try
            {
                if (task.IsCompleted)
                {
                    var snapshot = task.Result;

                    if (snapshot.Exists)
                    {
                        // Парсинг даних із документа
                        var winnerID = snapshot.GetValue<string>(WinnerID);
                        var date = snapshot.GetValue<DateTime>(Date);
                        var player1ID = snapshot.GetValue<string>(Player1ID);
                        var player1Elo = snapshot.GetValue<int>(Player1Elo);
                        var arrangementList1 =
                            ParseArrangement(snapshot.GetValue<Dictionary<string, object>>(ArrangementList1));
                        var player2ID = snapshot.GetValue<string>(Player2ID);
                        var player2Elo = snapshot.GetValue<int>(Player2Elo);
                        var arrangementList2 =
                            ParseArrangement(snapshot.GetValue<Dictionary<string, object>>(ArrangementList2));

                        var moveHistory =
                            ParseMoveList(snapshot.GetValue<List<Dictionary<string, object>>>(MoveHistory));
                        var player1Name = await GetPlayerName(player1ID);
                        var player2Name = await GetPlayerName(player2ID);
                        var historyMatchData = new HistoryMatchData
                        (
                            snapshot.Id,
                            winnerID,
                            date,
                            player1ID,
                            player1Elo,
                            player1Name,
                            arrangementList1,
                            player2ID,
                            player2Elo,
                            player2Name,
                            arrangementList2,
                            moveHistory
                        );
                        action?.Invoke(historyMatchData);
                    }
                    else
                    {
                        Debug.LogError($"Документ із ID {historyID} не знайдено.");
                    }
                }

                else if (task.IsFaulted)
                {
                    Debug.LogError("Помилка завантаження документа: " + task.Exception);
                }
            }
            catch (ArgumentNullException ex)
            {
                Debug.LogError("ArgumentNullException: " + ex.Message);
            }
        });
    }


    private ArrangementEntry[] ParseArrangement(Dictionary<string, object> arrangementData)
    {
        var result = new List<ArrangementEntry>();

        foreach (var item in arrangementData)
        {
            var entryData = item.Value as Dictionary<string, object>;

            if (entryData != null)
            {
                var arrangementEntry = new ArrangementEntry
                {
                    row = Convert.ToInt32(entryData[Row]),
                    column = Convert.ToInt32(entryData[Column]),
                    pieceType = Enum.Parse<PieceType>(entryData[PieceType].ToString())
                };

                result.Add(arrangementEntry);
            }
            else
            {
                Debug.LogError($"Помилка парсингу розстановки: невірний формат даних для ключа {item.Key}");
            }
        }

        return result.ToArray();
    }

    private List<int4> ParseMoveList(List<Dictionary<string, object>> moveData)
    {
        var result = new List<int4>();

        foreach (var moveDict in moveData)
        {
            var fromData = moveDict["From"] as Dictionary<string, object>;
            var toData = moveDict["To"] as Dictionary<string, object>;

            if (fromData != null && toData != null)
            {
                var move = new int4
                {
                    x = Convert.ToInt32(fromData[Row]),
                    y = Convert.ToInt32(fromData[Column]),
                    z = Convert.ToInt32(toData[Row]),
                    w = Convert.ToInt32(toData[Column])
                };

                result.Add(move);
            }
            else
            {
                Debug.LogError("Помилка розбору руху: формат даних неправильний.");
            }
        }

        return result;
    }

    #endregion
}