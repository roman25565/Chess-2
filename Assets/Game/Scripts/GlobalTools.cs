using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.Networking;


public static class GlobalTools
{
    private static List<CancellationTokenSource> _cts = new List<CancellationTokenSource>();

    public static async Task LoadSprite(Uri imageURL, Action<Sprite> callback)
    {
        if (imageURL == null)
        {
            callback.Invoke(null);
            return;
        }
        var request = UnityWebRequestTexture.GetTexture(imageURL);
        await request.SendWebRequest();
    
        if (request.result != UnityWebRequest.Result.Success) // Essential error handling
        {
            Debug.LogError("Error downloading texture: " + request.error);
            callback.Invoke(null);
            return;
        }
    
        var downloadedTexture = DownloadHandlerTexture.GetContent(request);
        var rect = new Rect(0, 0, downloadedTexture.width, downloadedTexture.height);
        var pivot = new Vector2(0.5f, 0.5f);
        var sprite = Sprite.Create(downloadedTexture, rect, pivot);
        callback.Invoke(sprite);
    }

    public static int CalculateNewRating(int currentRating, int opponentRating, double score, int kFactor = 32)
    {
        var expectedScore = 1 / (1 + Math.Pow(10, (opponentRating - currentRating) / 400.0));
        return (int)Math.Round(currentRating + kFactor * (score - expectedScore));
    }

    public static void Dispose()
    {
        foreach (var cts in _cts)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _cts.Clear();
    }
}