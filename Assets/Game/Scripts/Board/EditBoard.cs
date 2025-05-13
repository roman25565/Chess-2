using System.Collections.Generic;
using Setting;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace Board
{
    public class EditBoard : AbstractBoard
    {
        [SerializeField] private PlayerPanel myPlayerPanel;
        [SerializeField] private PlayerPanel enemyPlayerPanel;
#if !UNITY_SERVER
        [Inject] private Global _global;
     
        
        public override bool IsMyId(ulong id) => _matchData.MovingPlayerId == id;
        protected override bool IsRotated => _isRotated;
        
        private bool _isRotated;
        private MatchData _matchData;
        private bool _isHistoryMatch; 
        private MoveHistory _internalMoveHistory;
        private MoveHistory _selectedHistory;
        public override void ArrangeFigures(HistoryMatchData historyMatchData)
        {
#if !UNITY_SERVER
            ClearBoard();
#endif
            _isHistoryMatch = historyMatchData != null;
            if (_isHistoryMatch)
            {
                _matchData = new MatchData//first player is always white in history
                {
                    MovingPlayerId = 1,
                    Player1 = new PlayerData
                    {
                        Arrangement = null,
                        FirebasePlayer = null,
                        IsRotate = historyMatchData.Player1Id != _global.FirestoreManager.PlayerData.ID,
                        PlayerId = 1,
                        TimeToMove = -1,
                        IsMoving = true,
                        IsWhite = true,
                    },
                    Player2 = new PlayerData
                    {
                        Arrangement = null,
                        FirebasePlayer = null,
                        IsRotate = historyMatchData.Player2Id != _global.FirestoreManager.PlayerData.ID,
                        PlayerId = 2,
                        TimeToMove = -1,
                        IsMoving = false,
                        IsWhite = false,
                    }
                };
                Debug.Log($"Starting history match: {historyMatchData.Player1Id}, {historyMatchData.Player2Id}");
                ArrangeFigures(new MatchBootstrap.PlayerBootstrapData(
                    _matchData.Player1.PlayerId,
                    historyMatchData.Player1Id,
                    historyMatchData.Player1Arrangement,
                    _matchData.Player1.IsRotate,
                    _matchData.Player1.IsWhite));
                ArrangeFigures(new MatchBootstrap.PlayerBootstrapData(
                    _matchData.Player2.PlayerId,
                    historyMatchData.Player2Id,
                    historyMatchData.Player2Arrangement,
                    _matchData.Player2.IsRotate,
                    _matchData.Player2.IsWhite));
                _global.FirestoreManager.GetIcon(historyMatchData.Player1Id, (Sprite sprite) =>
                {
                    SetPlayerUI(new FirebasePlayerData(historyMatchData.Player1Id, historyMatchData.Player1Name,
                        historyMatchData.Player1Elo, sprite, null, null,null), historyMatchData.Player1Id != _global.FirestoreManager.PlayerData.ID);
                });
                
                _global.FirestoreManager.GetIcon(historyMatchData.Player2Id, (Sprite sprite) =>
                {
                    SetPlayerUI(new FirebasePlayerData(historyMatchData.Player2Id, historyMatchData.Player2Name,
                        historyMatchData.Player2Elo, sprite, null, null,null), historyMatchData.Player2Id != _global.FirestoreManager.PlayerData.ID);
                });

                _internalMoveHistory = new MoveHistory(Move);
                _selectedHistory = MoveHistory;
                MoveToEndMainHistory(historyMatchData.MoveHistory);
                MoveHistory.SetHistoryIndex(0);
                Debug.Log(MoveHistory.HistoryIndex);
            }
            else
            {
                
            }
            return;
            
            void SetPlayerUI(FirebasePlayerData playerData, bool isEnemyPlayer)
            {
                if (isEnemyPlayer) enemyPlayerPanel.SetPlayerUI(playerData);//TODO Save TimeControll
                else myPlayerPanel.SetPlayerUI(playerData);
            }
        }


        private void MoveToEndMainHistory(List<int4> moveHistory)
        {
            foreach (var int4 in moveHistory)
            {
                MovePiece(
                    GetCell(int4.x, int4.y), 
                    GetCell(int4.z, int4.w));
            }
        }

        protected override void BoardTryMove(Cell from, Cell to)
        {
            if (from.Piece.OwnerId == _matchData.MovingPlayerId)
            {
                if (_selectedHistory == MoveHistory) _selectedHistory = _internalMoveHistory;
                if (_selectedHistory == _internalMoveHistory)
                {
                    if (_selectedHistory.HistoryIndex < _selectedHistory.GetHistory().Count)
                    {
                        var list = _selectedHistory.GetHistory();
                        var index = _selectedHistory.HistoryIndex;
                        list.RemoveRange(index + 1, list.Count - (index + 1));
                    }
                }
                
                MovePiece(from, to, _selectedHistory, true);
            }
            
        }
        protected override void Move(Cell from, Cell to)
        {
            base.Move(from, to);
            Debug.Log("override void Move");
            var playerId = _matchData.Player1.IsMoving ? _matchData.Player1.PlayerId : _matchData.Player2.PlayerId;
            var anotherPlayer = _matchData.Player1.PlayerId == playerId ? _matchData.Player2 : _matchData.Player1;
            anotherPlayer.IsMoving = true;
            _matchData.MovingPlayerId = anotherPlayer.PlayerId;
            _matchData.GetPlayerData(playerId).IsMoving = false;
        }

        protected override void OnDraggingStop(Cell from, Cell to)
        {
            TryMove(to, false);
        }
        public override void NextMove()
        {
            if (!_selectedHistory.InHistory) return;
            _selectedHistory.SetHistoryIndex(_selectedHistory.HistoryIndex + 1);
        }

        public override void UndoMove()
        {
            _selectedHistory.SetHistoryIndex(_selectedHistory.HistoryIndex - 1);
            
            var index = _selectedHistory.HistoryIndex - 1;
            if (_selectedHistory == _internalMoveHistory && index < 0)
            {
                _internalMoveHistory.GetHistory().Clear();
                _selectedHistory = MoveHistory;
            }
        }
#else
        protected override void BoardTryMove(Cell from, Cell to)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnDraggingStop(Cell from, Cell to)
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}