using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    private PlayerInfo playerInfo;
    public event Action<PlayerInfo, string> OnSignedIn;
    private const string _accessToken = "_accessToken";

    private readonly JsonDataService dataService = new JsonDataService();
    private const string INSTALL_KEY = "/INSTALL_KEY.json";
    private const bool ENCRYPTION = true;
    bool isPendingInstallKey = false;
    public Action OnInstallKeyCreate;
    public Action OnSignInOptionActivate;

    private async void Awake()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        PlayerAccountService.Instance.SignedIn += SignInWithUnity;

        InstallKeyCheck();
        SignIn();
    }

    private void OnDestroy()
    {
        PlayerAccountService.Instance.SignedIn -= SignInWithUnity;
    }

    private void InstallKeyCheck()
    {
        //Work around for google auto saving data after reinstall cause playerprefs to load incorrectly
        //File.Delete(Application.persistentDataPath + INSTALL_KEY);
        if (!File.Exists(Application.persistentDataPath + INSTALL_KEY))
        {
            isPendingInstallKey = true;
            OnInstallKeyCreate?.Invoke();
            //debugText.text = "Install Key Created";
        }
        //else debugText.text = "Install Key Exists";
    }

    private void CreateInstallKey()
    {
        if (!isPendingInstallKey) return;
        bool isInstallKeyExist = true;
        dataService.SaveData(INSTALL_KEY, isInstallKeyExist, ENCRYPTION);
    }

    private async void SignIn()
    {
        if (PlayerPrefs.HasKey(_accessToken))
        {
            //debugText.text = "Access Token exists";
            await SignInAnonymously();
            await AuthenticationService.Instance.GetPlayerNameAsync();
            Debug.Log(AuthenticationService.Instance.PlayerName);
            CreateInstallKey();

        }
        else
        {
            await PlayerAccountService.Instance.StartSignInAsync();
            //debugText.text = "Access Token not exists";
            OnSignInOptionActivate?.Invoke();
        }
    }

    #region Update from Anonymous to Unity
    private async void LinkWithUnity()
    {
        try
        {
            var accessToken = PlayerAccountService.Instance.AccessToken;
            await LinkWithUnityAsync(accessToken);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task LinkWithUnityAsync(string accessToken)
    {
        try
        {
            //await AuthenticationService.Instance.DeleteAccountAsync();
            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
            Debug.Log("Link is successful.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");
        }

        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion

    #region Unity SignIn
    private async void SignInWithUnity()
    {
        try
        {
            var accessToken = PlayerAccountService.Instance.AccessToken;

            PlayerPrefs.SetString(_accessToken, accessToken);
            PlayerPrefs.Save();
            await SignInWithUnityAsync(accessToken);
            CreateInstallKey();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task SignInWithUnityAsync(string accessToken)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            CloudDataInitializer.Instance.CreateData();

            Debug.Log("SignIn is successful.");

            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

            OnSignedIn?.Invoke(playerInfo, name);
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    #endregion

    #region Anonymous SignIn
    public async Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            CloudDataInitializer.Instance.CreateData();
            //Debug.Log("Player Id: " + AuthenticationService.Instance.PlayerId);
        }
        catch (AuthenticationException ex)
        {
            Debug.Log("Sign in Failed");
            Debug.LogException(ex);
        }
    }
    #endregion
}
