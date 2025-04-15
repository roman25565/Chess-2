using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class GlobalTools
{
    public static async Task<Sprite> LoadSprite(Uri imageURL)
    {
        if (imageURL == null) return null;
        var request = UnityWebRequestTexture.GetTexture(imageURL);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) // Essential error handling
        {
            Debug.LogError("Error downloading texture: " + request.error);
            return null; // Or throw an exception if you prefer
        }

        var downloadedTexture = DownloadHandlerTexture.GetContent(request);
        var rect = new Rect(0, 0, downloadedTexture.width, downloadedTexture.height);
        var pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(downloadedTexture, rect, pivot);
    }

    public static int CalculateNewRating(int currentRating, int opponentRating, double score, int kFactor = 32)
    {
        var expectedScore = 1 / (1 + Math.Pow(10, (opponentRating - currentRating) / 400.0));
        return (int)Math.Round(currentRating + kFactor * (score - expectedScore));
    }
}