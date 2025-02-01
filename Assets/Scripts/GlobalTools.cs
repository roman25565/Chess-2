using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class GlobalTools
{
    public static async Task<Sprite> LoadSprite(System.Uri imageURL)
    {
        if (imageURL == null) return null;
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageURL);
        Debug.Log("Send Web Request");
        await request.SendWebRequest();
        Debug.Log("Sended Web Request");
        
        if (request.result != UnityWebRequest.Result.Success) // Essential error handling
        {
            Debug.LogError("Error downloading texture: " + request.error);
            return null; // Or throw an exception if you prefer
        }
        else
        {
            Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
            Rect rect = new Rect(0, 0, downloadedTexture.width, downloadedTexture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(downloadedTexture, rect, pivot);
        }
    }
    
    public static int CalculateNewRating(int currentRating, int opponentRating, double score, int kFactor = 32)
    {
        double expectedScore = 1 / (1 + Math.Pow(10, (opponentRating - currentRating) / 400.0));
        return (int)Math.Round(currentRating + kFactor * (score - expectedScore));
    }
}