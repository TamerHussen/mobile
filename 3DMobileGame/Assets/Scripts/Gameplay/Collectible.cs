using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Values")]
    public int scoreValue = 10;

    private bool collected = false;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerScore score = other.GetComponent<PlayerScore>();
        if (score == null)
        {
            Debug.LogWarning("Player has no PlayerScore component");
            return;
        }

        collected = true;

        score.AddScore(scoreValue);
        score.AddCollectible();

        Destroy(gameObject);
    }
}