using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class PushToTalkManager : MonoBehaviour
{
    public static PushToTalkManager Instance { get; private set; }

    [Header("Mic Settings")]
    [SerializeField] private string micDevice = null;   // default mic
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxLenSec = 10;

    private AudioClip recordingClip;
    private int startSample;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /* ---------- called by ArtifactButton ---------- */
    public void BeginRecord(string artifactKey)
    {
        if (Microphone.IsRecording(micDevice)) return;

        recordingClip = Microphone.Start(micDevice, false, maxLenSec, sampleRate);
        startSample = Microphone.GetPosition(micDevice);
        _currentKey = artifactKey;
    }

    public void EndRecord()
    {
        if (!Microphone.IsRecording(micDevice)) return;

        int endSample = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);

        int samples = endSample - startSample;
        if (samples <= 0) return;

        float[] data = new float[samples * recordingClip.channels];
        recordingClip.GetData(data, startSample);

        AudioClip trimmed = AudioClip.Create("UserClip", samples,
                                             recordingClip.channels,
                                             sampleRate, false);
        trimmed.SetData(data, 0);
        Debug.Log("end recording");
        // ship it
        NetworkManager.Instance.ChatWithArtifact(trimmed, _currentKey);
    }

    private string _currentKey;   // stored between begin ↔ end
}
