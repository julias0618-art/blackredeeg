using UnityEngine;
using LSL;

public class LSLMarkerSender : MonoBehaviour
{
    public bool enableLSL = true;
    public string lslStreamName = "UnityMarkers";
    public string lslStreamType = "Markers";
    public string lslSourceId = "unity_markers_001";
    public bool logToConsole = true;

    private StreamOutlet outlet;

    void Start()
    {
        if (!enableLSL) return;

        var info = new StreamInfo(lslStreamName, lslStreamType, 1, 0,
            channel_format_t.cf_string, lslSourceId);

        outlet = new StreamOutlet(info);

        if (logToConsole) Debug.Log($"[LSL] Stream started: {lslStreamName}");
    }

    public enum Phase { DECIDE, EXEC, RESET }

    // phase: DECIDE/EXEC/RESET
    // dirLabel: LEFT/RIGHT/UP/DOWN/ZOOM_IN/ZOOM_OUT 등 (RESET이면 빈 문자열 가능)
    public void SendSpaceMarker(Phase phase, int trialId, string dirLabel = "")
    {
        if (!enableLSL || outlet == null) return;

        double lslTs = LSL.LSL.local_clock();
        float unityRt = Time.realtimeSinceStartup;

        // 파싱 쉬운 key=value 포맷 추천
        // 예) SPACE|phase=EXEC|trial=42|dir=LEFT|unity_rt=...|lsl=...
        string marker =
            $"SPACE|phase={phase}|trial={trialId}" +
            (string.IsNullOrEmpty(dirLabel) ? "" : $"|dir={dirLabel}") +
            $"|unity_rt={unityRt:F6}|lsl={lslTs:F6}";

        outlet.push_sample(new[] { marker });

        if (logToConsole)
            Debug.Log($"[LSL] {marker}");
    }

    // --- 편의용 wrapper (원하면 사용) ---
    public void SendDecide(int trialId, string dirLabel) => SendSpaceMarker(Phase.DECIDE, trialId, dirLabel);
    public void SendExec(int trialId, string dirLabel) => SendSpaceMarker(Phase.EXEC, trialId, dirLabel);
    public void SendReset(int trialId) => SendSpaceMarker(Phase.RESET, trialId);
}
