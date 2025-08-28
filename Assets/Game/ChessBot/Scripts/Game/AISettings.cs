namespace Chess.Game
{
	using UnityEngine;
	using Chess.Core;

	[CreateAssetMenu(menuName = "AI/Settings")]
	public class AISettings : ScriptableObject
	{

		public event System.Action requestCancelSearch;

		[Header("Search")]
		public SearchSettings.SearchMode mode;
		public int searchTimeMillis = 1000;
		public int fixedSearchDepth;
		public bool runOnMainThread;


		[Header("Diagnostics")]
		public Searcher.SearchDiagnostics diagnostics;

		public void RequestCancelSearch()
		{
			requestCancelSearch?.Invoke();
		}
	}
}