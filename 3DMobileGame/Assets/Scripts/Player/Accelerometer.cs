using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Accelerometer : MonoBehaviour
{
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private PlayerController playerController;

    [SerializeField] private float shakeThreshold = 2.2f;

    [SerializeField] private float dodgeForce = 8f;
    [SerializeField] private float dodgeDuration = 0.18f;
    [SerializeField] private float dodgeCooldown = 1.2f;

    [Range(0.01f, 0.5f)]
    [SerializeField] private float lowPassFilterFactor = 0.1f;

    private bool canDodge = true;
    private Vector3 lowPassValue;

    void Start()
    {
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        lowPassValue = Input.acceleration;
    }

    void Update()
    {
        DetectShake();
    }

    private void DetectShake()
    {
        Vector3 accel = Input.acceleration;

        // low-pass to approximate gravity, then high-pass to remove it
        lowPassValue = Vector3.Lerp(lowPassValue, accel, lowPassFilterFactor);
        Vector3 highPass = accel - lowPassValue;
        float strength = highPass.magnitude;

        if (strength > shakeThreshold && canDodge)
        {
            TriggerDodge();
        }
    }

    private void TriggerDodge()
    {
        canDodge = false;

        // Dodge forward relative to player transform
        Vector3 forwardDir = transform.forward;
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.001f) forwardDir = Vector3.forward;

        if (playerController != null)
        {
            playerController.StartDodge(forwardDir, dodgeForce, dodgeDuration);
        }
        else
        {
            // fallback direct velocity change
            playerRigidbody.AddForce(forwardDir.normalized * dodgeForce, ForceMode.VelocityChange);
        }

        Handheld.Vibrate();

        Invoke(nameof(ResetDodge), dodgeCooldown);
    }

    private void ResetDodge()
    {
        canDodge = true;
    }
}
