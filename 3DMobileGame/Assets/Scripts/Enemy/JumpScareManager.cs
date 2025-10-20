using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class JumpScare
{
    public Sprite image;
    public AudioClip audio;
}

public class JumpScareManager : MonoBehaviour
{
    public Image jumpscareUI;   
    public AudioSource audioSource; 
    public JumpScare[] jumpscares;

    public float displayTime = 3.5f;  // How long the image stays
    public float scareCooldown = 5f;  // Delay before another jumpscare can trigger

    private bool canScare = true;

    public void TriggerRandomJumpScare()
    {
        if (!canScare || jumpscares.Length == 0) return;
        canScare = false;

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
        Invoke(nameof(ResetScare), scareCooldown);

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
