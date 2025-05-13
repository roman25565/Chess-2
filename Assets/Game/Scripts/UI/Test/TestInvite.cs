using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Setting;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.Test
{
public class TestInvite : MonoBehaviour
{
#if !UNITY_SERVER
    [Inject] private Global _global;
    public Button create;
    public Button connect;
    public string id;
    public string playerId;
    public string ip;
    public ushort port;
    
    
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private ClientMatchmaker clientMatchmaker;


    private CancellationTokenSource cts;
    
    public void Init()
    { 
        // _global.FirestoreManager.OnLogin.AddListener(Auth);
    }

    private void OnDestroy()
    {
        // _global.FirestoreManager.OnLogin.RemoveListener((() => _ = Auth()));
    }

    private void Auth()
    {
        // Debug.Log(AuthenticationService.Instance.PlayerId);
        // playerId = AuthenticationService.Instance.PlayerId;
        // create.onClick.AddListener(() => _ = Create());
        // connect.onClick.AddListener(Connect);
    }
    

    private async Task Create()
    {
        mainMenu.EnableFindMatchPanel();
        
        cts = new CancellationTokenSource();
        
        if (cts.Token.IsCancellationRequested)
        {
            Debug.LogWarning("Search already in progress");
            return;
        }
        
        var players = new List<Player>
        {
            new(AuthenticationService.Instance.PlayerId),
            new(playerId),
        };

        var attributes = new Dictionary<string, object>();
        var queueName = "test";
        var options = new CreateTicketOptions(queueName, attributes);

        try
        {
            var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            id= ticketResponse.Id;
            await clientMatchmaker.FindMatch(id, (arg0, arg1) =>
            {
                Debug.Log("arg0:" + arg0 + " arg1:" + arg1);
                ip = arg0;
                port = arg1;
            });
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void Connect()
    {
        Debug.Log("Connect");
        mainMenu.EnableFindMatchPanel();
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetConnectionData(ip, port);
            NetworkManager.Singleton.StartClient();
    }
#endif
}
}