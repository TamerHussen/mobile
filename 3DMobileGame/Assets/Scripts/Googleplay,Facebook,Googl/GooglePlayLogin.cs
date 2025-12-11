using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEditor.Experimental.GraphView;
using TMPro;

public class GooglePlayLogin : MonoBehaviour
{
    public TextMeshProUGUI DetailsText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SignIn();
    }

    // Update is called once per frame
    void SignIn()
    {
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            // continue with play games service
            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            string id = PlayGamesPlatform.Instance.GetUserId();
            string ImgUrl = PlayGamesPlatform.Instance.GetUserImageUrl();

            DetailsText.text = "Success \n " + name;

        }
        else
        {
            DetailsText.text = "Sign in Failed!";
            // disable your integration with play games services or show a login button
            // to ask users to sin in. clicking it should call
            // playergamesplatform.instance.manuallyauthenticate(processauthentication)
        }
    }
}
