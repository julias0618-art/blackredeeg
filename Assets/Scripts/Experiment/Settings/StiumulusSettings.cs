   using UnityEngine;

[CreateAssetMenu(menuName = "MI/Stimulus Settings")]
public class StimulusSettings : ScriptableObject
{
    [Header("Timing (seconds)")]
    public float interTrialSec = 6.0f;
    public float cueSec = 1.2f;
    public float targetSec = 4.0f;

    [Header("XR (Quest)")]
    public bool isXR = false; // XR용 프리셋 구분용 플래그
}
