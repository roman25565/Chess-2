using UnityEngine;
#if !UNITY_SERVER
using System;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Setting;
using Statistics;
using Zenject;
#endif
namespace UI
{

public class PlayerProfileController : MonoBehaviour
{

    [Inject] private Global _global;
   

    private void UpdateProfile(FirebasePlayerData player)
    {
        
    }

    private void DrawEloGraph(List<HistoryMatchData> matches)
    {
        
    }
}

}