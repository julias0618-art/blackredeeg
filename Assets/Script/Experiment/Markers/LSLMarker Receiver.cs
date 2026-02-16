using UnityEngine;
using LSL;
using System.Threading;
using System.Collections.Concurrent;

public class LSLMarkerReceiver : MonoBehaviour
{
    public string listenStreamName = "CppToUnityMarkers";
    public StimulusController stimulus;  // ✅ 인스펙터에 연결

    StreamInlet inlet;
    Thread thread;
    volatile bool running;

    readonly ConcurrentQueue<string> q = new ConcurrentQueue<string>();

    void Start()
    {
        Debug.Log("### RECEIVER START CALLED ###");
        running = true;
        thread = new Thread(WorkerLoop) { IsBackground = true };
        thread.Start();
        Debug.Log("[LSL RX] Looking for: " + listenStreamName);

        var all = LSL.LSL.resolve_stream("type", "Markers", 20, 2.0);
        Debug.Log($"[LSL] Found {all.Length} marker streams:");
        for (int i = 0; i < all.Length; i++)
            Debug.Log($"  {i}: name={all[i].name()} source_id={all[i].source_id()}");

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
                q.Enqueue("[LSL RX] Subscribed");
            }

            string[] sample = new string[1];
            double ts = inlet.pull_sample(sample, 0.2);
            if (ts != 0.0 && sample[0] != null)
                q.Enqueue(sample[0]);
        }
    }

    void Update()
    {
        while (q.TryDequeue(out var msg))
        {
            Debug.Log("[LSL RX] " + msg);

            // ✅ C++에서 보낸 SPACE_DOWN만 StimulusController에 전달
            if (msg.StartsWith("SPACE_DOWN"))
            {
                stimulus?.TriggerSpaceFromLSL();
            }
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
