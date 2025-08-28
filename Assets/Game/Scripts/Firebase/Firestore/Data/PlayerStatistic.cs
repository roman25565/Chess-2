#if !UNITY_SERVER
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using Board;
using Board.Piece;
using Game.Scripts.Board;
using Setting;
using UnityEngine;

namespace Statistics
{
[FirestoreData]
public class PlayerStatistic
{
    [FirestoreProperty] public Timestamp RegistrationDate { get; set; }
    [FirestoreProperty] public Timestamp LastPlayedDate { get; set; }

    [FirestoreProperty] public int CurrentEloRating { get; set; }
    [FirestoreProperty] public int PeakEloRating { get; set; }
    [FirestoreProperty] public int LowestEloRating { get; set; }

    public int TotalMatchesPlayed => Wins + Losses + Draws;
    [FirestoreProperty] public int Wins { get; set; }
    [FirestoreProperty] public int Losses { get; set; }
    [FirestoreProperty] public int Draws { get; set; }
    public float WinRate => TotalMatchesPlayed > 0 ? (Wins / (float)TotalMatchesPlayed) * 100 : 0;

    [FirestoreProperty] public int WinsAsWhite { get; set; }
    [FirestoreProperty] public int WinsAsBlack { get; set; }
    [FirestoreProperty] public int LossesAsWhite { get; set; }
    [FirestoreProperty] public int LossesAsBlack { get; set; }

    [FirestoreProperty] public long TotalPlayTimeHours { get; set; }

    public TimeSpan TotalPlayTime
    {
        get => TimeSpan.FromHours(TotalPlayTimeHours);
        set => TotalPlayTimeHours = (long)value.TotalHours;
    }

    [FirestoreProperty] public int TotalMovesMade { get; set; }
    public int AverageMovesPerGame => TotalMatchesPlayed > 0 ? TotalMovesMade / TotalMatchesPlayed : 0;

    [FirestoreProperty] public int KingsDefeated { get; set; }
    [FirestoreProperty] public int KingsLost { get; set; }
    [FirestoreProperty] public int Resignations { get; set; }
    [FirestoreProperty] public int Timeouts { get; set; }
    [FirestoreProperty] public int DrawsByAgreement { get; set; }
    [FirestoreProperty] public int CurrentWinStreak { get; set; }
    [FirestoreProperty] public int MaxWinStreak { get; set; }
    [FirestoreProperty] public int CurrentLoseStreak { get; set; }
    [FirestoreProperty] public int MaxLoseStreak { get; set; }


    public DateTime GetRegistrationDate() => RegistrationDate.ToDateTime().ToLocalTime();
    public DateTime GetLastPlayedDate() => LastPlayedDate.ToDateTime().ToLocalTime();

    public void UpdateStatistics(PlayerData playerData, List<Move> history,
        EndGameType endGameType, WonReason wonReason)
    {
        LastPlayedDate = Timestamp.FromDateTime(DateTime.UtcNow);

        CurrentEloRating = playerData.FirebasePlayer.PlayerRanking.Elo;
        PeakEloRating = PeakEloRating < CurrentEloRating ? CurrentEloRating : PeakEloRating;
        LowestEloRating = LowestEloRating > CurrentEloRating ? CurrentEloRating : LowestEloRating;

        Debug.Log($"UpdateStatistics: {CurrentEloRating}, {PeakEloRating}, {LowestEloRating}");
        
        switch (endGameType)
        {
            case EndGameType.Won:
                Wins += 1;
                break;
            case EndGameType.Lose:
                Losses += 1;
                break;
            case EndGameType.Draw:
                Draws += 1;
                break;
        }

        if (playerData.IsWhite)
        {
            switch (endGameType)
            {
                case EndGameType.Won:
                    WinsAsWhite += 1;
                    break;
                case EndGameType.Lose:
                    LossesAsWhite += 1;
                    break;
            }
        }
        else
        {
            switch (endGameType)
            {
                case EndGameType.Won:
                    WinsAsBlack += 1;
                    break;
                case EndGameType.Lose:
                    LossesAsBlack += 1;
                    break;
            }
        }

        float secondsDiff = playerData.StartTimeToMove - playerData.TimeToMove;
        if (secondsDiff > 0)
        {
            TotalPlayTimeHours += (long)(secondsDiff / 3600f);
        }
        else
        {
            Debug.LogWarning($"Negative time difference: {secondsDiff} seconds");
        }

        TotalMovesMade += history.Count / 2;

        switch (endGameType)
        {
            case EndGameType.Draw:
                DrawsByAgreement += 1;
                break;
        }

        switch (wonReason)
        {
            case WonReason.Surrender:
                Resignations += 1;
                break;
            case WonReason.Timeouts:
                Timeouts += 1;
                break;
        }
        
        List<AbstractPiece> allKilledKings = new();
        foreach (var move in history)
        {
            if (move.KilledPiece != null && move.KilledPiece.PieceType == PieceType.King)
            {
                allKilledKings.Add(move.KilledPiece);;
            }
        }

        var kingsDefeated = 0;
        var kingsLost = 0;

        var isWhite = playerData.IsWhite;
        
        foreach (var king in allKilledKings)
        {
            bool isKingWhite = king.Color == PieceColor.White;
            bool shouldCountAsDefeated = isKingWhite != isWhite;
    
            if (shouldCountAsDefeated)
                kingsDefeated++;
            else
                kingsLost++;
        }
        
        KingsDefeated += kingsDefeated;
        KingsLost += kingsLost;

        switch (endGameType)
        {
            case EndGameType.Won:
                CurrentLoseStreak = 0;
                CurrentWinStreak += 1;
                MaxWinStreak = Math.Max(MaxWinStreak, CurrentWinStreak);
                break;
            
            case EndGameType.Lose:
                CurrentWinStreak = 0;
                CurrentLoseStreak += 1;
                MaxLoseStreak = Math.Max(MaxLoseStreak, CurrentLoseStreak);
                break;
        }
    }
}
}
#endif
