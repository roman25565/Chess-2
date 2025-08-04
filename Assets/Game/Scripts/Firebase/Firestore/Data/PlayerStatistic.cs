#if !UNITY_SERVER
using Firebase.Firestore;
using System;

namespace Statistics
{
    [FirestoreData]
    public class PlayerStatistic
    {
        [FirestoreProperty] public Timestamp RegistrationDate { get; set; }
        [FirestoreProperty] public Timestamp LastPlayedDate { get; set; }
        
        // Рейтинги
        [FirestoreProperty] public int CurrentEloRating { get; set; }
        [FirestoreProperty] public int PeakEloRating { get; set; }
        [FirestoreProperty] public int LowestEloRating { get; set; }

        // Загальна статистика
        public int TotalMatchesPlayed => Wins + Losses + Draws;
        [FirestoreProperty] public int Wins { get; set; }
        [FirestoreProperty] public int Losses { get; set; }
        [FirestoreProperty] public int Draws { get; set; }
        public float WinRate => TotalMatchesPlayed > 0 ? (Wins / (float)TotalMatchesPlayed) * 100 : 0;
        
        // Деталізація по часу
        [FirestoreProperty] public int WinsAsWhite { get; set; }
        [FirestoreProperty] public int WinsAsBlack { get; set; }
        [FirestoreProperty] public int LossesAsWhite { get; set; }
        [FirestoreProperty] public int LossesAsBlack { get; set; }
        
        // Для TimeSpan використовуємо long (секунди)
        [FirestoreProperty] public long TotalPlayTimeSeconds { get; set; }
        public TimeSpan TotalPlayTime 
        {
            get => TimeSpan.FromSeconds(TotalPlayTimeSeconds);
            set => TotalPlayTimeSeconds = (long)value.TotalSeconds;
        }

        // Статистика по ходах
        [FirestoreProperty] public int TotalMovesMade { get; set; }
        public int AverageMovesPerGame => TotalMatchesPlayed > 0 ? TotalMovesMade / TotalMatchesPlayed : 0;

        // Стиль гри
        [FirestoreProperty] public int CheckmatesGiven { get; set; }
        [FirestoreProperty] public int CheckmatesReceived { get; set; }
        [FirestoreProperty] public int Resignations { get; set; }
        [FirestoreProperty] public int Timeouts { get; set; }
        [FirestoreProperty] public int DrawsByAgreement { get; set; }
        [FirestoreProperty] public int DrawsByRepetition { get; set; }
        [FirestoreProperty] public int DrawsByStalemate { get; set; }

        // Серії
        [FirestoreProperty] public int CurrentWinStreak { get; set; }
        [FirestoreProperty] public int MaxWinStreak { get; set; }
        [FirestoreProperty] public int CurrentLoseStreak { get; set; }
        [FirestoreProperty] public int MaxLoseStreak { get; set; }
        

        // Методи для роботи з датами
        public DateTime GetRegistrationDate() => RegistrationDate.ToDateTime().ToLocalTime();
        public DateTime GetLastPlayedDate() => LastPlayedDate.ToDateTime().ToLocalTime();
        public void SetRegistrationDate(DateTime date) => RegistrationDate = Timestamp.FromDateTime(date);
        public void SetLastPlayedDate(DateTime date) => LastPlayedDate = Timestamp.FromDateTime(date);
    }
}
#endif
