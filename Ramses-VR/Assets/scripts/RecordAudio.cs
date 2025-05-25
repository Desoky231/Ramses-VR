using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Recorder : MonoBehaviour
{
    [SerializeField] private string micDevice = null;   // leave null = default
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int maxLengthSec = 30;

    private AudioClip recordedClip;
    private bool isRecording;

    public void StartRecording()
    {
        if (isRecording) return;

        recordedClip = Microphone.Start(micDevice, false, maxLengthSec, sampleRate);
        isRecording = true;
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(micDevice);
        isRecording = false;

        // Fire-and-forget – NetworkManager handles the rest
        NetworkManager.Instance.ChatWithAudio(recordedClip);
    }

    // Optional local playback for debugging
    public void PlayRecording(AudioSource src)
    {
        if (recordedClip == null) return;

        src.clip = recordedClip;
        src.Play();
    }
}
