using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// Hold right trigger to talk; release to send.
/// Safer version – no false “micro-taps”.
[RequireComponent(typeof(AudioSource))]
public class RecorderVR : MonoBehaviour
{
    [Header("Microphone")]
    [SerializeField] private string micDevice = null;
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int maxLengthSec = 30;
    [SerializeField] private float minValidLengthSec = 0.25f;   // ← NEW

    private AudioClip recordedClip;
    private bool isRecording = false;
    private Coroutine autoStopCo = null;

    private InputDevice rightHand = default;
    private bool btnHeld = false;
    private double recordStartTime;

    /* ─────────── device discovery ─────────── */
    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChange;
        InputDevices.deviceDisconnected += OnDeviceChange;
        FindRightHand();
    }
    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChange;
        InputDevices.deviceDisconnected -= OnDeviceChange;
    }
    private void OnDeviceChange(InputDevice _) => FindRightHand();
    private void FindRightHand()
    {
        var list = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right |
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.HeldInHand,
            list);
        rightHand = list.Count > 0 ? list[0] : default;
        if (rightHand.isValid)
            Debug.Log($"[RecorderVR] Using {rightHand.name}");
    }

    /* ─────────── poll trigger button ─────────── */
    private void Update()
    {
        if (!rightHand.isValid) { FindRightHand(); return; }

        if (!rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
            return;     // controller lacks that feature

        if (!btnHeld && pressed)                  // button down
        {
            BeginRecording();
            btnHeld = true;
        }
        else if (btnHeld && !pressed)             // button up
        {
            StopRecordingAndSend();
            btnHeld = false;
        }
    }

    /* ─────────── start / stop ─────────── */
    private void BeginRecording()
    {
        if (isRecording) return;

        recordedClip = Microphone.Start(micDevice, false, maxLengthSec, sampleRate);
        if (recordedClip == null) { Debug.LogError("[RecorderVR] Mic failed"); return; }

        isRecording = true;
        recordStartTime = AudioSettings.dspTime;
        autoStopCo = StartCoroutine(AutoStopRecording());
        Debug.Log("[RecorderVR] ► REC");
    }

    private IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(maxLengthSec);
        if (isRecording)
        {
            Debug.Log("[RecorderVR] Auto-stop (time limit).");
            StopRecordingAndSend();
        }
    }

    private void StopRecordingAndSend()
    {
        if (!isRecording) return;

        int samplesRecorded = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;
        if (autoStopCo != null) StopCoroutine(autoStopCo);

        /* Ignore accidental taps (< minValidLengthSec) */
        double seconds = samplesRecorded / (double)sampleRate;
        if (seconds < minValidLengthSec)
        {
            Debug.LogWarning($"[RecorderVR] Discarded {seconds:F3}s – too short");
            return;
        }

        float[] data = new float[samplesRecorded * recordedClip.channels];
        recordedClip.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create("Trimmed", samplesRecorded,
                                             recordedClip.channels,
                                             recordedClip.frequency, false);
        trimmed.SetData(data, 0);

        NetworkManager.Instance.ChatWithAudio(trimmed);
        Debug.Log($"[RecorderVR] ► SENT {seconds:F2}s");
    }
}
