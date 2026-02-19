using UnityEngine;
using LSL;
using System.Threading;
using System.Collections.Concurrent;

public class LSLDirectionReceiver : MonoBehaviour
{
    public string listenStreamName = "CppToUnityDirections";
    public QuestCameraController cameraController; // connect in Inspector

    StreamInlet inlet;
    Thread thread;
    volatile bool running;

    readonly ConcurrentQueue<string> q = new ConcurrentQueue<string>();

    void Start()
    {
        running = true;
        thread = new Thread(WorkerLoop) { IsBackground = true };
        thread.Start();
        Debug.Log("[LSL RX] Looking for direction stream: " + listenStreamName);
    }

    void WorkerLoop()
    {
        while (running)
        {
            if (inlet == null)
            {
                var results = LSL.LSL.resolve_stream("name", listenStreamName, 1, 2.0);
                if (results.Length == 0) { Thread.Sleep(300); continue; }
                inlet = new StreamInlet(results[0]);
                q.Enqueue("[LSL RX] Direction subscribed");
            }

            string[] sample = new string[1];
            double ts = inlet.pull_sample(sample, 0.2);
            if (ts != 0.0 && !string.IsNullOrEmpty(sample[0]))
                q.Enqueue(sample[0]);
        }
    }

    void Update()
    {
        while (q.TryDequeue(out var msg))
        {
            Debug.Log("[LSL DIR] " + msg);

            // simplest case: message is just "Left"/"Right"/"Up"/"Down"/"ZOOM_IN"/"ZOOM_OUT"
            if (cameraController != null)
                cameraController.OnInferredDirection(msg);
        }
    }

    void OnDestroy()
    {
        running = false;
        thread?.Join(300);
        try { inlet?.close_stream(); } catch { }
        inlet = null;
    }
}
