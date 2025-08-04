#if !UNITY_SERVER
using UnityEngine;
using UI;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;

using Google;
using Setting;
using TMPro;
using Zenject;

namespace Bootstrap
{
public enum SignTypes
{
    None = 0,
    Google = 1,
    Anonymous = 2,
}
public class SignIn : MonoBehaviour
{
    [Inject] private Global _global;
    
    private const string WebClientId = "492940055939-57m8n1fr0eu5cgis5kn94p1kj310cm4f.apps.googleusercontent.com";
    
    
    private GoogleSignInConfiguration _configuration;
    private const string SignTypeKey = "SignType";
    [SerializeField] private Bootstrap bootstrap;
    [SerializeField] private MainMenu mainMenu;
    public void Init(bool isSignIn = false)
    {
        _configuration = new GoogleSignInConfiguration
        {
            WebClientId = WebClientId,
            UseGameSignIn = false,
            RequestEmail = true
        };
        if(isSignIn) //if return from Game Scene
        {
            mainMenu.DisableSignInPanel();
        }
        else if (LoadLastSignType(out var type) != SignTypes.None) // if ReLogin
        {
            OnSignInGoogle();
            mainMenu.DisableSignInPanel();
        }
    }
    
    public void OnSignInGoogle()
    {

        GoogleSignIn.Configuration = _configuration;
        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(
            OnAuthenticationFinished);

    }
    
    public void OnSignInDebug()
    {
        var user = new GoogleSignInUser
        {
            UserId = "001",
            ImageUrl = new Uri(
                "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no"),
            Email = "<EMAIL>", DisplayName = "Alpha",
        }; 
        bootstrap.OnSignInDebug(user);
        UpdateUI(user);
    }
    
    public void OnSignInDebug2()
    {
        var user = new GoogleSignInUser
        {
            UserId = "002",
            ImageUrl = new Uri(
                "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no"),
            Email = "<EMAIL>", DisplayName = "Beta",
        }; 
        bootstrap.OnSignInDebug(user);
        UpdateUI(user);
    }
    
    public void OnSignInDebug3()
    {
        var user = new GoogleSignInUser
        {
            UserId = "003",
            ImageUrl = new Uri(
                "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no"),
            Email = "<EMAIL>", DisplayName = "Omega",
        }; 
        bootstrap.OnSignInDebug(user);
        UpdateUI(user);
    }
    
    public void OnSignOut()
    {

            mainMenu.ShowSignInPanel();
            
            LoadLastSignType(out var type);
            Debug.Log("Calling SignOut");
            if (type == SignTypes.Google)
            {
                try
                {
                    GoogleSignIn.DefaultInstance.SignOut();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
            }
            
            PlayerPrefs.SetString(SignTypeKey, SignTypes.None.ToString());
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
        {
            var user = task.Result;
            string defaultImageUrl =
                "https://lh3.googleusercontent.com/a/ACg8ocL4wCFqm80fhBx6h117v0DUgjklmq84dmQf6ViCtsv01_y3W9zq";
            if (user.ImageUrl == null)
            {
                Debug.LogError("user.ImageUrl is null");
                user.ImageUrl = new Uri(defaultImageUrl);
            }
            
            UpdateUI(user);
            bootstrap.OnSignIn(user);
            PlayerPrefs.SetString(SignTypeKey, SignTypes.Google.ToString());
            Debug.Log("success");
        }
        Debug.Log("OnAuthenticationFinished?");
    }
    
    private void UpdateUI(GoogleSignInUser user)
    {
        try
        {
            Debug.Log("Welcome: " + user.DisplayName + "!!!!!");

            mainMenu.DisableSignInPanel();
            
            GlobalTools.LoadSprite(user.ImageUrl, (Sprite sprite) =>
            {
                mainMenu.SetProfileImage(sprite);
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }
    


    private SignTypes LoadLastSignType(out SignTypes type)
    {
        var savedValue = PlayerPrefs.GetString(SignTypeKey, SignTypes.None.ToString());
        if (Enum.TryParse(savedValue, out SignTypes loadedSignType))
        {
            Debug.Log("Завантажено SignTypes: " + loadedSignType);
        }
        else
        {
            Debug.LogError("Не вдалося завантажити SignTypes. Використовується значення за замовчуванням.");
            loadedSignType = SignTypes.None;
        }

        type = loadedSignType;
        return loadedSignType;
    }
}
}
#endif   
