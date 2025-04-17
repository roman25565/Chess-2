using System;
using System.Threading.Tasks;
using Firebase.Extensions;
using Google;
using Setting;
using TMPro;
using UI;
using UnityEngine;
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
    
    [SerializeField] private Bootstrap bootstrap;
    [SerializeField] private MainMenu mainMenu;
    
    private GoogleSignInConfiguration _configuration;
    private const string SignTypeKey = "SignType";
    
    public void Init()
    {
        _configuration = new GoogleSignInConfiguration
        {
            WebClientId = WebClientId,
            UseGameSignIn = false,
            RequestEmail = true
        };
        if(_global != null && _global.IsSignIn) //if return from Game Scene
        {
            mainMenu.DisableSignInPanel();
            mainMenu.DisableSignInPanel();
            return;
        }else if (LoadLastSignType(out var type) != SignTypes.None) // if ReLogin
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
        bootstrap.OnSignInDebug("001");
        UpdateUI(new GoogleSignInUser{UserId = "001"});
    }
    
    public void OnSignOut()
    {
        try
        {
            mainMenu.EnableSignInPanel();
            
            Debug.Log("Calling SignOut");
            GoogleSignIn.DefaultInstance.SignOut();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
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
            UpdateUI(task.Result);
            bootstrap.OnSignIn(task.Result);
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
            
            _global.FirestoreManager.GetIcon(user.UserId, (Sprite sprite) =>
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