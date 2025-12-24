using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadScreen;
    public Image loadingSpinner;

    private void Awake()
    {
        loadScreen.SetActive(false);
    }

    public void Show()
    {
        loadScreen.SetActive(true);
    }

    public void Hide()
    {
        loadScreen.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        if (loadingSpinner != null)
            loadingSpinner.fillAmount = progress;
    }
}
