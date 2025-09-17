using Setting;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Zenject;

namespace Bootstrap
{
using UnityEngine;
using System.Collections;

public class ReconnectFetcher : MonoBehaviour
{
    [Inject] Global _global;
    [SerializeField] private int fetchAttempts = 5;
    [SerializeField] private float delayBetweenAttempts = 3f;
    private UnityAction _func;
    private Coroutine _coroutine;
    private bool _isFetching;
    
    public void Init()
    {
        _global.BackendManager.OnLogin.AddListener(() =>
            StartFetching(() => _global.BackendManager.RealtimeDatabase.ReConnectRequestsManager.FetchReConnectRequests())
        );
        _global.BackendManager.OnSignOut.AddListener(StopFetching);
    }
    private void StartFetching(UnityAction func)
    {
        if (_isFetching) StopFetching();
        _isFetching = true;
        _func = func;
        _coroutine = StartCoroutine(FetchReconnectRequestsRoutine());
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
    
    private IEnumerator FetchReconnectRequestsRoutine()
    {
        int attempts = 0;
        
        while (attempts < fetchAttempts)
        {
            _func();
            attempts++;
            
            if (attempts < fetchAttempts)
            {
                yield return new WaitForSeconds(delayBetweenAttempts);
            }
        }
    }

}
}