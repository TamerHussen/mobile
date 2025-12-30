using UnityEngine;
using System.Collections.Generic;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<System.Action> dispatchQueue = new Queue<System.Action>();

    void Update()
    {
        lock (dispatchQueue)
        {
            while (dispatchQueue.Count > 0)
            {
                dispatchQueue.Dequeue().Invoke();
            }
        }
    }

    public static void Enqueue(System.Action action)
    {
        lock (dispatchQueue)
        {
            dispatchQueue.Enqueue(action);
        }
    }
}
