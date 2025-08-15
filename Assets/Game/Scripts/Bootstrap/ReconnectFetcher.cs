using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Bootstrap
{
using UnityEngine;
using System.Collections;

public class ReconnectFetcher : MonoBehaviour
{
    [SerializeField] private int fetchAttempts = 5;
    [SerializeField] private float delayBetweenAttempts = 3f;
    private UnityAction _func;
    
    public void StartFetching(UnityAction func)
    {
        _func = func;
        StartCoroutine(FetchReconnectRequestsRoutine());
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