using UnityEngine;
using LSL;

public class LSLMarkerSender : MonoBehaviour
{
    public bool enableLSL = true;
    public string lslStreamName = "UnityMarkers";
    public string lslStreamType = "Markers";
    public string lslSourceId   = "unity_markers_001";
    public bool logToConsole = true;

    StreamOutlet outlet;

    void Start()
    {
        if (!enableLSL) return;

        var info = new StreamInfo(lslStreamName, lslStreamType, 1, 0,
            channel_format_t.cf_string, lslSourceId);

        outlet = new StreamOutlet(info);

        if (logToConsole) Debug.Log($"[LSL] Stream started: {lslStreamName}");
    }

    // ✅ 스페이스 누른 "그 순간" 보내기 (중복 제한 없음)
    public void SendOnSpace(string trialLabel)
    {
        if (!enableLSL || outlet == null) return;

        double lslTs = LSL.LSL.local_clock();
        float  unityRt = Time.realtimeSinceStartup;

        // 한 줄 문자열로: 라벨 + 시간 2개
        string marker = $"SPACE|{trialLabel}|unity_rt={unityRt:F6}|lsl={lslTs:F6}";

        outlet.push_sample(new[] { marker });

        if (logToConsole)
            Debug.Log($"[LSL] {marker}");
    }
}
