using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRInputDump : MonoBehaviour
{
    InputDevice left, right;
    readonly List<InputFeatureUsage<bool>> boolUsages = new()
    {
        CommonUsages.primaryButton,    // X / A
        CommonUsages.secondaryButton,  // Y / B
        CommonUsages.gripButton,
        CommonUsages.triggerButton,
        CommonUsages.menuButton,
        CommonUsages.primaryTouch,
        CommonUsages.secondaryTouch
    };

    void Start()
    {
        RefreshDevices();
    }

    void RefreshDevices()
    {
        var devs = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            devs);
        if (devs.Count > 0) left = devs[0];

        devs.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devs);
        if (devs.Count > 0) right = devs[0];
    }

}
