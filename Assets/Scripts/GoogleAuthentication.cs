using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Google;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class GoogleAuthentication : MonoBehaviour
{
    public TextMeshProUGUI userNameTxt, userEmailTxt;
    private GoogleSignInConfiguration configuration;
    private const string webClientId = "492940055939-57m8n1fr0eu5cgis5kn94p1kj310cm4f.apps.googleusercontent.com";

    void Awake()
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
#if UNITY_EDITOR
        userEmailTxt.text = "asdasasdasdasd" ;
        userNameTxt.text = "111111";
        return;
#endif
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
            OnAuthenticationFinished);
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        Debug.Log(task.Result);
        if (task.IsFaulted)
        {
            using (IEnumerator<System.Exception> enumerator =
                task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    GoogleSignIn.SignInException error =
                        (GoogleSignIn.SignInException)enumerator.Current;
                    Debug.LogError("Got Error: " + error.Status + " " + error.Message);
                }
                else
                {
                    Debug.LogError("Got unexpected exception?!?" +  task.Exception);
                }
            }
        }
        else if (task.IsCanceled)
        {
            Debug.LogError("Cancelled");
        }
        else
        {
            UpdateUI(task.Result);
        }
        Debug.Log("OnAuthenticationFinished?");
    }

    private void UpdateUI(GoogleSignInUser user)
    {
        Debug.Log("          Welcome: " + user.DisplayName + "!!!!!");

        userEmailTxt.text = "asda             dsas                dasdasdas                    d" + user.Email;
        userNameTxt.text = user.DisplayName;
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
#if UNITY_EDITOR
        userEmailTxt.text = "" ;
        userNameTxt.text = "";
        return;
#endif
        userNameTxt.text = "";
        userEmailTxt.text = "";
    
        // imageURL = "";
        // loginPanel.SetActive(true);
        // profilePanel.SetActive(false);
        Debug.Log("Calling SignOut");
        GoogleSignIn.DefaultInstance.SignOut();
    }

}
