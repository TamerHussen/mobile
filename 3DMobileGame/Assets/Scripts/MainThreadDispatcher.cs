using UnityEngine;
using System.Collections.Generic;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private static readonly Queue<System.Action> dispatchQueue = new Queue<System.Action>();
    private static readonly object queueLock = new object();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("MainThreadDispatcher initialized");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // process everything in queue each frame
        lock (queueLock)
        {
            while (dispatchQueue.Count > 0)
            {
                try
                {
                    System.Action action = dispatchQueue.Dequeue();
                    action?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"MainThreadDispatcher error: {e.Message}");
                }
            }
        }
    }

    public static void Enqueue(System.Action action)
    {
        if (action == null)
        {
            Debug.LogWarning("Tried to enqueue null action");
            return;
        }

        lock (queueLock)
        {
            dispatchQueue.Enqueue(action);
            Debug.Log($"Action enqueued. Queue size: {dispatchQueue.Count}");
        }
    }

    // backup method to ensure instance exists
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            GameObject dispatcher = new GameObject("MainThreadDispatcher");
            instance = dispatcher.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(dispatcher);
        }
    }
}