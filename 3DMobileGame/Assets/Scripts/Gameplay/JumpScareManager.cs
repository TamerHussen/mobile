using UnityEngine;
using UnityEngine.UI;

public class JumpScareManager : MonoBehaviour
{
    public static JumpScareManager Instance;

    [Header("UI")]
    public Image jumpscareUI;
    public AudioSource audioSource;

    [Header("Display Settings")]
    public float displayTime = 3f;
    public float scareCooldown = 5f;

    private bool canScare = true;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (LevelUIManager.Instance == null)
        {
            Debug.LogError("LevelUIManager not initialized BEFORE JumpScareManager");
            return;
        }

        if (jumpscareUI == null)
            Debug.LogError("Jumpscare Image not assigned in LevelUIManager!");

        audioSource = GetComponent<AudioSource>();
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
            jumpscareUI.gameObject.SetActive(false);
            Debug.Log("JumpscareImage hidden at start");
        }
    }

    public void TriggerCustomJumpscare(Sprite image, AudioClip audio)
    {
        if (!canScare) return;
        if (image == null || jumpscareUI == null) return;

        Debug.Log($"Triggering jumpscare: {image.name}");

        canScare = false;

        jumpscareUI.sprite = image;
        jumpscareUI.gameObject.SetActive(true);

        if (audio != null && audioSource != null)
        {
            audioSource.clip = audio;
            audioSource.Play();
            Debug.Log($"Playing jumpscare audio: {audio.name}");
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
            jumpscareUI.gameObject.SetActive(false);
            Debug.Log("Jumpscare hidden");
        }
    }

    private void ResetScare()
    {
        canScare = true;
        Debug.Log("Jumpscare cooldown complete - ready for next scare");
    }
}