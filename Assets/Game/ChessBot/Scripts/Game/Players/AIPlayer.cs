using System;
	using System.Threading.Tasks;
	using System.Threading;
	using Chess.Core;
	using Chess.Game;
	using UnityEngine;
	using SearchSettings = Chess.Core.SearchSettings;

namespace Chess.Players
{

	public class AIPlayer : Player
	{

		public Searcher search;
		AISettings settings;
		bool moveFound;
		Move move;
		public Core.Board board;
		CancellationTokenSource cancelSearchTimer;
		System.Random rng;

		OpeningBook book;

		public AIPlayer(Core.Board board, AISettings settings)
		{
			this.settings = settings;
			this.board = board;
			rng = new System.Random();
			settings.requestCancelSearch += TimeOutThreadedSearch;

			search = new Searcher(board, CreateSearchSettings(settings));
			search.onSearchComplete += OnSearchComplete;
			search.searchDiagnostics = new Searcher.SearchDiagnostics();

		}
		
		private bool FastTryKillKing()
		{
			var result = false;
			var moves = search.GetMoves();
			foreach (var move in moves)
			{
				int capturedPieceType = Piece.PieceType(board.Square[move.TargetSquare]);
				if (capturedPieceType == Piece.King || capturedPieceType == Piece.BlackKing || capturedPieceType == Piece.WhiteKing)
				{
					Debug.Log($"Fast kill king {capturedPieceType}");
					result = true;
					ChoseMove(move);
					break;
				}
			}
			return result;
		}

		// Update running on Unity main thread. This is used to return the chosen move so as
		// not to end up on a different thread and unable to interface with Unity stuff.
		public override void Update()
		{

			if (moveFound)
			{
				Debug.Log($"move found {move.Value}");
				settings.diagnostics = search.searchDiagnostics;
				moveFound = false;
				ChoseMove(move);
			}

			settings.diagnostics = search.searchDiagnostics;

		}

		public void AbortSearch()
		{
			search.EndSearch();
		}

		public override void NotifyTurnToMove()
		{
			Debug.Log("NotifyTurnToMove");
			try
			{
				search.searchDiagnostics.isBook = false;
				moveFound = false;

				if (FastTryKillKing()) return;

				if (settings.runOnMainThread)
				{
					StartSearch();
				}
				else
				{
					StartThreadedSearch();

				}
			}
			catch (Exception D)
			{
				Debug.LogError(D);
				Console.WriteLine(D);
				throw;
			}

		}

		void StartSearch()
		{
			search.StartSearch();
			moveFound = true;
		}

		void StartThreadedSearch()
		{
			Task.Factory.StartNew(() => search.StartSearch(), TaskCreationOptions.LongRunning);

			if (settings.mode != SearchSettings.SearchMode.FixedDepth)
			{
				cancelSearchTimer = new CancellationTokenSource();
				Task.Delay(settings.searchTimeMillis, cancelSearchTimer.Token).ContinueWith((t) => TimeOutThreadedSearch());
			}

		}

		// Note: called outside of Unity main thread
		void TimeOutThreadedSearch()
		{
			if (cancelSearchTimer == null || !cancelSearchTimer.IsCancellationRequested)
			{
				search.EndSearch();
			}
		}

		void PlayBookMove(Move bookMove)
		{
			this.move = bookMove;
			moveFound = true;
		}

		SearchSettings CreateSearchSettings(AISettings aISettings)
		{
			return new SearchSettings();
		}
		void OnSearchComplete(Move move)
		{
			// Cancel search timer in case search finished before timer ran out (can happen when a mate is found)
			cancelSearchTimer?.Cancel();
			moveFound = true;
			this.move = move;
		}
	}
}