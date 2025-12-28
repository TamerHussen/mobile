using UnityEngine;
using UnityEngine.UI;

public class JumpScareManager : MonoBehaviour
{
    [Header("UI")]
    public Image jumpscareUI;
    public AudioSource audioSource;

    [Header("Display Settings")]
    public float displayTime = 3.5f;
    public float scareCooldown = 5f;

    private bool canScare = true;

    void Awake()
    {
        Debug.Log(" JumpScareManager Awake()");

        if (jumpscareUI == null)
        {
            jumpscareUI = GameObject.Find("JumpscareImage")?.GetComponent<Image>();
            if (jumpscareUI != null)
            {
                Debug.Log(" Auto-found JumpscareImage");
            }
            else
            {
                Debug.LogError(" JumpscareImage not found! Looking for GameObject named 'JumpscareImage'");
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                Debug.Log(" AudioSource found on JumpScareManager");
            }
        }
    }

    void Start()
    {
        if (audioSource != null)
        {
            audioSource.enabled = true;
            audioSource.playOnAwake = false;
        }

        if (jumpscareUI != null)
        {
            jumpscareUI.enabled = false;
            Debug.Log(" JumpscareImage hidden at start");
        }
    }

    public void TriggerCustomJumpscare(Sprite image, AudioClip audio)
    {
        if (!canScare)
        {
            Debug.Log(" Jumpscare on cooldown");
            return;
        }

        if (image == null)
        {
            Debug.LogWarning(" No jumpscare image provided!");
            return;
        }

        if (jumpscareUI == null)
        {
            Debug.LogError(" jumpscareUI is null! Cannot show jumpscare!");
            return;
        }

        Debug.Log($" ========== TRIGGERING JUMPSCARE: {image.name} ==========");

        canScare = false;

        jumpscareUI.sprite = image;
        jumpscareUI.enabled = true;

        Debug.Log($" Jumpscare image set and enabled");

        if (audio != null && audioSource != null)
        {
            audioSource.enabled = true;
            audioSource.clip = audio;
            audioSource.Play();
            Debug.Log($" Playing jumpscare audio: {audio.name}");
        }
        else if (audio == null)
        {
            Debug.LogWarning("⚠No audio clip provided for jumpscare");
        }

        Invoke(nameof(HideJumpScare), displayTime);
        Invoke(nameof(ResetScare), scareCooldown);

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        Debug.Log("Vibration triggered");
#endif
    }

    private void HideJumpScare()
    {
        if (jumpscareUI != null)
        {
            jumpscareUI.enabled = false;
            Debug.Log("Jumpscare hidden");
        }
    }

    private void ResetScare()
    {
        canScare = true;
        Debug.Log("Jumpscare cooldown complete - ready for next scare");
    }
}