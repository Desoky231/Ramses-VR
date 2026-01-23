using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


public class ControlPanelToggle : MonoBehaviour
{
    [Header("Object To Toggle")]
    [Tooltip("العنصر الذى سيتم إظهاره/إخفاؤه عند الضغط على زر Y")]
    [SerializeField] private GameObject target;

    /* ───────────── Input Helpers ───────────── */
    private InputDevice leftHand = default;
    private bool yHeld = false; // Debounce  

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
        var devs = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left |
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.HeldInHand, devs);

        leftHand = devs.Count > 0 ? devs[0] : default;

        if (leftHand.isValid)
            Debug.Log($"[ControlPanelToggle] Using {leftHand.name} (left hand)");
    }

    private void Update()
    {
        if (!leftHand.isValid) { FindLeftHand(); return; }

        // Y button == secondaryButton على يد Oculus/Meta اليسرى
        if (!leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed)) return;

        if (!yHeld && pressed)           // زر ضُغط الآن
        {
            ToggleTarget();
            yHeld = true;
        }
        else if (yHeld && !pressed)      // تم رفع الإصبع عن الزر
        {
            yHeld = false;
        }
    }

    /* ───────────── Toggle Logic ───────────── */
    private void ToggleTarget()
    {
        if (target == null)
        {
            Debug.LogWarning("[ControlPanelToggle] لا يوجد Target مُحدد!");
            return;
        }

        bool newState = !target.activeSelf;
        target.SetActive(newState);
        Debug.Log($"[ControlPanelToggle] {(newState ? "Activated" : "Deactivated")} → {target.name}");
    }
}
