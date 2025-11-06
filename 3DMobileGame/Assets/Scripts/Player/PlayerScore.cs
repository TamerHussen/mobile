using UnityEngine;
using TMPro;

public class PlayerScore : MonoBehaviour
{
    public int Score = 0;
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI CollectedText;
    public int Collected = 0;
    public int MaxCollectibles;

    private void Start()
    {

        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        MaxCollectibles = collectibles.Length;

        UpdateScoreUI();
        UpdateCollectedUI();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            Score += 10;
            Collected++;

            UpdateScoreUI();
            UpdateCollectedUI();

            Destroy(other.gameObject);
        }
    }

    void UpdateScoreUI()
    {
        if (ScoreText != null)
            ScoreText.text = "Score: " + Score;
    }

    void UpdateCollectedUI()
    {
        if (CollectedText != null)
            CollectedText.text = $"Collected: {Collected} / {MaxCollectibles}";
    }
}
