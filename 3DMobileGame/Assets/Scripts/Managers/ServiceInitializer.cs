using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using System.Threading.Tasks;
using UnityEngine;

public static class ServiceInitializer
{
    static bool initialized;

    public static async Task Initialize()
    {
        if (initialized) return;

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        await FriendsService.Instance.InitializeAsync();

        initialized = true;
        Debug.Log("Services initialized");
    }
}
