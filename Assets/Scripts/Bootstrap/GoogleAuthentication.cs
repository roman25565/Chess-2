using System;
using System.Threading.Tasks;
using Firebase.Extensions;
using Google;
using TMPro;
using UnityEngine;

public class GoogleAuthentication : MonoBehaviour
{
    private const string webClientId = "492940055939-57m8n1fr0eu5cgis5kn94p1kj310cm4f.apps.googleusercontent.com";
    public TextMeshProUGUI userNameTxt, userEmailTxt;
    public Bootstrap.Bootstrap bootstrap;
    public GameObject signInPanel;
    private GoogleSignInConfiguration configuration;

    private void Awake()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            UseGameSignIn = false,
            RequestEmail = true
        };
    }

    public void OnSignIn()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(
            OnAuthenticationFinished);
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
            using (var enumerator =
                   task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var error =
                        (GoogleSignIn.SignInException)enumerator.Current;
                    Debug.LogError("Got Error: " + error.Status + " " + error.Message);
                }
                else
                {
                    Debug.LogError("Got unexpected exception?!?" + task.Exception);
                }
            }
        else if (task.IsCanceled)
            Debug.LogError("Cancelled");
        else
            try
            {
                UpdateUI(task.Result);
                bootstrap.OnSignIn(task.Result);
                Debug.Log("success");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }

        Debug.Log("OnAuthenticationFinished?");
    }

    private void UpdateUI(GoogleSignInUser user)
    {
        try
        {
            Debug.Log("Welcome: " + user.DisplayName + "!!!!!");

            userEmailTxt.text = user.Email;
            userNameTxt.text = user.DisplayName;
            signInPanel.SetActive(false);
            Debug.Log("else sdasd");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
        // Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
        // Rect rect = new Rect(0,0, downloadedTexture.width, downloadedTexture.height);
        // Vector2 pivot = new Vector2(0.5f, 0.5f);
        // profilePic.sprite = Sprite.Create(downloadedTexture, rect, pivot);
        //
        // loginPanel.SetActive(false);
        // profilePanel.SetActive(true);
    }

    public void OnSignOut()
    {
        try
        {
            signInPanel.SetActive(true);
            userNameTxt.text = "";
            userEmailTxt.text = "";

            // imageURL = "";
            // loginPanel.SetActive(true);
            // profilePanel.SetActive(false);
            Debug.Log("Calling SignOut");
            GoogleSignIn.DefaultInstance.SignOut();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }

    public void OnSignInDebug()
    {
        signInPanel.SetActive(false);
        bootstrap.OnSignIn("001");
    }
}