using UnityEngine;

public class CameraWallClip : MonoBehaviour
{
    [Header("Settings")]
    public float minDistance = 0.5f;
    public float maxDistance = 3f;
    public float smoothSpeed = 10f;
    public LayerMask collisionLayers;

    private Transform playerTransform;
    private Vector3 desiredLocalPosition;
    private float currentDistance;

    void Start()
    {
        playerTransform = transform.parent;
        if (playerTransform == null)
        {
            Debug.LogError(" Camera must be child of Player!");
            enabled = false;
            return;
        }

        desiredLocalPosition = transform.localPosition;
        currentDistance = desiredLocalPosition.magnitude;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 desiredWorldPos = playerTransform.TransformPoint(desiredLocalPosition);
        RaycastHit hit;

        if (Physics.Raycast(playerTransform.position, desiredWorldPos - playerTransform.position,
            out hit, currentDistance, collisionLayers))
        {
            currentDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, maxDistance);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, desiredLocalPosition.magnitude,
                Time.deltaTime * smoothSpeed);
        }

        Vector3 direction = desiredLocalPosition.normalized;
        transform.localPosition = Vector3.Lerp(transform.localPosition, direction * currentDistance,
            Time.deltaTime * smoothSpeed);
    }
}