using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Server : MonoBehaviour
{
    string _ticketId;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(StartServer());
        //StartCoroutine(ApproveBackfillTicketEverySecond());
    }

    async Awaitable StartServer()
    {
        await UnityServices.InitializeAsync();
        var server = MultiplayService.Instance.ServerConfig;
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", server.Port);
        Debug.Log("Network Transport " + transport.ConnectionData.Address + ":" + transport.ConnectionData.Port);

        if (!NetworkManager.Singleton.StartServer())
        {
            Debug.LogError("Failed to start server");
            throw new Exception("Failed to start server");
        }

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => { Debug.Log("Client connected"); };
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnServerStopped += (reason) => { Debug.Log("Server stopped"); };
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        Debug.Log($"Started Server {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");

        var callbacks = new MultiplayEventCallbacks();
        callbacks.Allocate += OnAllocate;
        callbacks.Deallocate += OnDeallocate;
        callbacks.Error += OnError;
        callbacks.SubscriptionStateChanged += OnSubscriptionStateChanged;

        while (MultiplayService.Instance == null)
        {
            await Awaitable.NextFrameAsync();
        }

        // We must then subscribe.
        var events = await MultiplayService.Instance.SubscribeToServerEventsAsync(callbacks);
        await CreateBackfillTicket();
    }

    private static void OnClientDisconnect(ulong clientId)
    {
        Debug.Log("Client disconnected");
        if (NetworkManager.Singleton.ConnectedClients.Count == 0)
        {
            NetworkManager.Singleton.Shutdown();
            Application.Quit();
        }
    }

    void OnSubscriptionStateChanged(MultiplayServerSubscriptionState obj)
    {
        Debug.Log($"Subscription state changed: {obj}");
    }

    void OnError(MultiplayError obj)
    {
        Debug.Log($"Error received: {obj}");
    }

    async void OnDeallocate(MultiplayDeallocation obj)
    {
        Debug.Log($"Deallocation received: {obj}");
        await MultiplayService.Instance.UnreadyServerAsync();
    }

    async void OnAllocate(MultiplayAllocation allocation)
    {
        Debug.Log($"Allocation received: {allocation}");
        await MultiplayService.Instance.ReadyServerForPlayersAsync();
    }

    async Task CreateBackfillTicket()
    {
        MatchmakingResults results =
            await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

        Debug.Log(
            $"Environment: {results.EnvironmentId} MatchId: {results.MatchId} MatchProperties: {results.MatchProperties}");

        var backfillTicketProperties = new BackfillTicketProperties(results.MatchProperties);

        string queueName = "test"; // must match the name of the queue you want to use in matchmaker
        string connectionString = MultiplayService.Instance.ServerConfig.IpAddress + ":" +
                                  MultiplayService.Instance.ServerConfig.Port;

        var options = new CreateBackfillTicketOptions(queueName,
            connectionString,
            new Dictionary<string, object>(),
            backfillTicketProperties);

        // Create backfill ticket
        Debug.Log("Requesting backfill ticket");
        _ticketId = await MatchmakerService.Instance.CreateBackfillTicketAsync(options);
    }

    IEnumerator ApproveBackfillTicketEverySecond()
    {
        for (int i = 4; i >= 0; i--)
        {
            Debug.Log($"Waiting {i} seconds to start backfill");
            yield return new WaitForSeconds(1f);
        }

        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (String.IsNullOrWhiteSpace(_ticketId))
            {
                Debug.Log("No backfill ticket to approve");
                continue;
            }

            Debug.Log("Doing backfill approval for _ticketId: " + _ticketId);
            yield return MatchmakerService.Instance.ApproveBackfillTicketAsync(_ticketId);
            Debug.Log("Approved backfill ticket: " + _ticketId);
        }
    }
}