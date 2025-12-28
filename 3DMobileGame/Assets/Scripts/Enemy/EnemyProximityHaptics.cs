using UnityEngine;

public class EnemyProximityHaptics : MonoBehaviour
{
    public Transform player;
    public EnemyBehaviorSystem enemy;

    public float maxDistance = 10f;
    public float minInterval = 0.2f;
    public float maxInterval = 1f; 

    private float vibrationTimer = 0f;

    void Update()
    {
        if (player == null || enemy == null) return;

        float distance = Vector3.Distance(player.position, enemy.transform.position);

        if (distance > maxDistance) return;

        float t = Mathf.Clamp01(distance / maxDistance);
        float interval = Mathf.Lerp(minInterval, maxInterval, t);

        vibrationTimer -= Time.deltaTime;
        if (vibrationTimer <= 0f)
        {
            TriggerVibration();
            vibrationTimer = interval;
        }
    }

    private void TriggerVibration()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
