using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages jumpscares triggered by enemy behavior system
/// Attach to: JumpScareManager GameObject in scene
/// </summary>
public class JumpScareManager : MonoBehaviour
{
    [Header("UI")]
    public Image jumpscareUI;
    public AudioSource audioSource;

    [Header("Display Settings")]
    public float displayTime = 3.5f;
    public float scareCooldown = 5f;

    private bool canScare = true;

    /// <summary>
    /// Trigger a specific jumpscare (called by enemy)
    /// </summary>
    public void TriggerCustomJumpscare(Sprite image, AudioClip audio)
    {
        if (!canScare)
        {
            Debug.Log("Jumpscare on cooldown");
            return;
        }

        if (image == null)
        {
            Debug.LogWarning("No jumpscare image provided!");
            return;
        }

        canScare = false;

        // Show image
        jumpscareUI.sprite = image;
        jumpscareUI.enabled = true;

        // Play audio
        if (audio != null && audioSource != null)
        {
            audioSource.clip = audio;
            audioSource.Play();
        }

        // Hide after delay
        Invoke(nameof(HideJumpScare), displayTime);
        Invoke(nameof(ResetScare), scareCooldown);

        Debug.Log("Jumpscare triggered!");

        // Vibrate device
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    private void HideJumpScare()
    {
        jumpscareUI.enabled = false;
    }

    private void ResetScare()
    {
        canScare = true;
    }
}
