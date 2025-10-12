#if !UNITY_SERVER
using UnityEngine;
using UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;

using Google;
using Setting;
using TMPro;
using Unity.Services.Authentication;
using Unity.VisualScripting;
using UnityEngine.UI;
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
    
    private const string WebClientId = "118320481974-t1bo9evo0p6ee3evrrn4k7j6t6675u6c.apps.googleusercontent.com";
    const string AnonymouslyIdKey = "AnonymousIdKey";
    
    private GoogleSignInConfiguration _configuration;
    private const string SignTypeKey = "SignType";
    [SerializeField] private Bootstrap bootstrap;
    [SerializeField] private MainMenu mainMenu;
    
    [SerializeField] private Button signInGoogleButton;
    [SerializeField] private Button signInDebugButton;
    [SerializeField] private Button signInDebug2Button;
    [SerializeField] private Button signInDebug3Button;
    [SerializeField] private Button signInAnonymouslyButton;
    
    [SerializeField] private Button signOutButton;

    public void Init(bool isSignIn = false)
    {
        SubscribeButtons();
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
            if (type == SignTypes.Google)
            {
                OnSignInGoogle();
                mainMenu.DisableSignInPanel();
            }
            else if (type == SignTypes.Anonymous)
            {
                var id = PlayerPrefs.GetString(AnonymouslyIdKey);
                var user = new GoogleSignInUser
                {
                    UserId = id,
                };
                bootstrap.OnSignInDebug(user);
                UpdateUI(user);
            }
        }
        
    }

    private void SubscribeButtons()
    {
        signInGoogleButton.onClick.AddListener(OnSignInGoogle);
        signInDebugButton.onClick.AddListener(OnSignInDebug);
        signInDebug2Button.onClick.AddListener(OnSignInDebug2);
        signInDebug3Button.onClick.AddListener(OnSignInDebug3);
        signInAnonymouslyButton.onClick.AddListener(OnSignUpAnonymously);
        signOutButton.onClick.AddListener(OnSignOut);
    }
    
    private void OnSignInGoogle()
    {
        GoogleSignIn.Configuration = _configuration;
        try
        {
            GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(
                OnAuthenticationFinished);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error signing in: {e.Message}");
            Console.WriteLine(e);
            throw;
        }
    }
    
    private void OnSignInDebug()
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
    
    private void OnSignInDebug2()
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
    
    private void OnSignInDebug3()
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

    private void OnSignUpAnonymously()
    {
        var url = GetRandomImageUrl();
        _global.BackendManager.PlayerDataManager.OnSignInAnonymously(url, id =>
        {
            var user = new GoogleSignInUser
            {
                UserId = id,
                ImageUrl = new Uri(url),
                Email = "<EMAIL>", DisplayName = "Omega",
            }; 
            bootstrap.OnSignInAnonymously(user);
            UpdateUI(user);
            PlayerPrefs.SetString(SignTypeKey, SignTypes.Anonymous.ToString());
            PlayerPrefs.SetString(AnonymouslyIdKey, id);
        });
    }

    private string GetRandomImageUrl()
    {
        return "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no";
    }

    private void OnSignOut()
    {
        _global.BackendManager.OnSignOut?.Invoke();
        mainMenu.ShowSignInPanel();
        AuthenticationService.Instance.SignOut();
        LoadLastSignType(out var type);
        PlayerPrefs.SetString(SignTypeKey, SignTypes.None.ToString());
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

        
    }



    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            Debug.LogError("Google Sign-In faulted!");

            foreach (var inner in task.Exception.InnerExceptions)
            {
                if (inner is GoogleSignIn.SignInException gse)
                {
                    Debug.LogError(
                        $"[GoogleSignIn] Status: {(int)gse.Status}\n" +
                        $"Message: {gse.Message}\n" +
                        $"StackTrace: {gse.StackTrace}");
                }
                else
                {
                    Debug.LogError($"[GoogleSignIn] Unexpected exception: {inner}");
                }
            }
            
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
        }
        else if (task.IsCanceled)
            Debug.LogError("Cancelled");
        else
        {
            var user = task.Result;
            // Credential credential = GoogleAuthProvider.GetCredential(user.IdToken, null);
            // var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            // auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
            // {
            //     if (authTask.IsCanceled) { }else if (authTask.IsFaulted) { }else { }
            // });
            string defaultImageUrl =
                "https://lh3.googleusercontent.com/a/ACg8ocL4wCFqm80fhBx6h117v0DUgjklmq84dmQf6ViCtsv01_y3W9zq";
            if (user.ImageUrl == null)
            {
                Debug.LogError("user.ImageUrl is null");
                user.ImageUrl = new Uri(defaultImageUrl);
            }
            
            UpdateUI(user);
            PlayerPrefs.SetString(SignTypeKey, SignTypes.Google.ToString());
            bootstrap.OnSignIn(user, SignTypes.Google);
            Debug.Log("success");
        }
        Debug.Log("OnAuthenticationFinished?");
    }
    
    private void UpdateUI(GoogleSignInUser user)
    {
        mainMenu.DisableSignInPanel();
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
        Debug.Log("LoadLastSignType " + loadedSignType);
        return loadedSignType;
    }
}
}
#endif   
