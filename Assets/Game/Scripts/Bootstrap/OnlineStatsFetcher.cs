using System.Collections;
using System.Collections.Generic;
using Setting;
using UI;
using UnityEngine;
using Zenject;

public class OnlineStatsFetcher : MonoBehaviour
{
    [Inject] private Global _global;
    [SerializeField] private float updateInterval = 5f;
    [SerializeField] private OnlineStatsUI onlineStatsUI;
    [SerializeField] private AdvancedMatchmaking advancedMatchmaking;
    private Coroutine _coroutine;
    private bool _isFetching;

    public void Init(bool isSignIn = false)
    {
        _global.BackendManager.OnLogin.AddListener(StartFetching);
        _global.BackendManager.OnSignOut.AddListener(StopFetching);
        if (isSignIn)
        {
            StartFetching();
        }
    }


    private void StartFetching()
    {
        if (_isFetching) StopFetching();
        _isFetching = true;
        _coroutine = StartCoroutine(FetchRoutine());
    }

    private void StopFetching()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        _isFetching = false;
    }

    private IEnumerator FetchRoutine()
    {
        while (true)
        {
            UpdateStats();
            yield return new WaitForSeconds(updateInterval);
        }
    }


    private async void UpdateStats()
    {
        var result = new Dictionary<int, int> { { 1, 0 }, { 5, 0 }, { 10, 0 } };
        
        await advancedMatchmaking.GetLobbiesCount(result);
        onlineStatsUI.UpdateUI(result);
    }
}