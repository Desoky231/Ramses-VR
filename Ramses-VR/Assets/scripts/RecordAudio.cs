using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// Hold X (left controller) to record; release to send.
[RequireComponent(typeof(AudioSource))]
public class RecorderVR : MonoBehaviour
{
    /* ───────────── Microphone ───────────── */
    [Header("Microphone")]
    [SerializeField] private string micDevice = null;
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int maxLengthSec = 30;
    [SerializeField] private float minValidLengthSec = 0.25f;

    private AudioClip recordedClip;
    private bool isRecording = false;
    private Coroutine autoStopCo = null;

    /* ───────────── XR input ───────────── */
    private InputDevice leftHand = default;      // ← now LEFT hand
    private bool btnHeld = false;

    /* ───────────── Device discovery ───────────── */
    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChange;
        InputDevices.deviceDisconnected += OnDeviceChange;
        FindLeftHand();
    }
    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChange;
        InputDevices.deviceDisconnected -= OnDeviceChange;
    }
    private void OnDeviceChange(InputDevice _) => FindLeftHand();

    private void FindLeftHand()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left |
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.HeldInHand,
            devices);

        leftHand = devices.Count > 0 ? devices[0] : default;
        if (leftHand.isValid)
            Debug.Log($"[RecorderVR] Using {leftHand.name} (left hand)");
    }

    /* ───────────── Poll X button ───────────── */
    private void Update()
    {
        if (!leftHand.isValid) { FindLeftHand(); return; }

        // X button = primaryButton on LEFT controller
        if (!leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed))
            return;                                         // controller lacks that feature

        if (!btnHeld && pressed)                           // button down
        {
            BeginRecording();
            btnHeld = true;
        }
        else if (btnHeld && !pressed)                      // button up
        {
            StopRecordingAndSend();
            btnHeld = false;
        }
    }

    /* ───────────── Start / Stop recording ───────────── */
    private void BeginRecording()
    {
        if (isRecording) return;

        recordedClip = Microphone.Start(micDevice, false, maxLengthSec, sampleRate);
        if (recordedClip == null)
        {
            Debug.LogError("[RecorderVR] Mic failed");
            return;
        }

        isRecording = true;
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

        double seconds = samplesRecorded / (double)sampleRate;
        if (seconds < minValidLengthSec)
        {
            Debug.LogWarning($"[RecorderVR] Discarded {seconds:F3}s – too short");
            return;
        }

        float[] data = new float[samplesRecorded * recordedClip.channels];
        recordedClip.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create("Trimmed",
                                             samplesRecorded,
                                             recordedClip.channels,
                                             recordedClip.frequency,
                                             false);
        trimmed.SetData(data, 0);

        NetworkManager.Instance.ChatWithAudio(trimmed);
        Debug.Log($"[RecorderVR] ► SENT {seconds:F2}s");
    }
}
