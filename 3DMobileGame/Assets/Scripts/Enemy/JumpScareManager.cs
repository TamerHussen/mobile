using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class JumpScare
{
    public Sprite image;       // Image for the jumpscare
    public AudioClip audio;    // Sound for the jumpscare
}

public class JumpScareManager : MonoBehaviour
{
    public Image jumpscareUI;       // Fullscreen UI Image for jumpscare
    public AudioSource audioSource; // AudioSource to play sound
    public JumpScare[] jumpscares;

    public float displayTime = 3.5f;  // How long the image stays

    public void TriggerRandomJumpScare()
    {
        if (jumpscares.Length == 0) return;

        JumpScare selected = jumpscares[Random.Range(0, jumpscares.Length)];

        jumpscareUI.sprite = selected.image;
        jumpscareUI.enabled = true;

        if (selected.audio != null && audioSource != null)
        {
            audioSource.clip = selected.audio;
            audioSource.Play();
        }

        // Hide after delay
        Invoke(nameof(HideJumpScare), displayTime);

    #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
    #endif
    }

    private void HideJumpScare()
    {
        jumpscareUI.enabled = false;
    }
}
