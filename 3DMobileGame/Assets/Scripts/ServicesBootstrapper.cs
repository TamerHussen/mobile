using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using System.Threading.Tasks;

public class ServicesBootstrapper : MonoBehaviour
{
    public static ServicesBootstrapper Instance { get; private set; }
    public static bool IsReady { get; private set; }

    async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await Initialize();
    }

    async Task Initialize()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        await FriendsService.Instance.InitializeAsync();

        IsReady = true;
        Debug.Log("Unity + Auth + Friends READY");
    }
}
