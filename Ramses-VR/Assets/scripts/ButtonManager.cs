using UnityEngine;
using UnityEngine.EventSystems;   // for IPointerDown/UpHandler

/// <summary>
/// Attach to a UI Button (or any UI element) to record while held
/// and send the clip to FastAPI /chat-with-artifact when released.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ButtonManager : MonoBehaviour,
                                IPointerDownHandler,
                                IPointerUpHandler
{
    [Header("Artifact Settings")]
    [Tooltip("Key used by FastAPI's ARTIFACT_DB (e.g. \"EA19\")")]
    [SerializeField] private string artifactName = "EA19";

    [Header("Recording Settings")]
    [SerializeField] private string microphoneDevice = null;   // null = default
    [SerializeField] private int sampleRate = 16000;  // Whisper's sweet spot
    [SerializeField] private int maxLengthSeconds = 10;     // hard cap

    // internals
    private AudioClip recordingClip;
    private int recordingStartSample;

    /* ───────────────────────────────────────────── */
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Microphone.IsRecording(microphoneDevice)) return;

        recordingClip = Microphone.Start(microphoneDevice,
                                              false,
                                              maxLengthSeconds,
                                              sampleRate);
        recordingStartSample = Microphone.GetPosition(microphoneDevice);
        Debug.Log("🎙️  Recording started …");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!Microphone.IsRecording(microphoneDevice)) return;

        int endSample = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);                         // stop capture

        int totalSamples = endSample - recordingStartSample;
        if (totalSamples <= 0)
        {
            Debug.LogWarning("Recorded nothing.");
            return;
        }

        // Trim clip to actual recorded length
        float[] data = new float[totalSamples * recordingClip.channels];
        recordingClip.GetData(data, recordingStartSample);

        AudioClip trimmed =
            AudioClip.Create("UserRecording",
                             totalSamples,
                             recordingClip.channels,
                             sampleRate,
                             false);
        trimmed.SetData(data, 0);

        Debug.Log("📤 Sending to FastAPI …");
        NetworkManager.Instance.ChatWithArtifact(
            trimmed,
            artifactName,
            onDone: (transcript, response) =>
            {
                Debug.Log($"✅ Done.\nUser: {transcript}\nAI  : {response}");
            },
            onError: err => Debug.LogError($"❌ {err}")
        );
    }
}
