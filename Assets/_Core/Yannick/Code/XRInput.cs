﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using UnityEngine;
using UnityEngine.XR;

public enum XRInputAxis { JOYSTICK_X, JOYSTICK_Y, TRIGGER, GRIP}
public enum XRInputButtons { MENU, PRIMARY, SECONDARY }

public class XRInput : MonoBehaviour
{

    public static XRInput Instance => instance;

    private static XRInput instance;

    private InputDevice leftController, rightController;
    private List<InputDevice> allDevices = new List<InputDevice>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        GetDevices(true, true);
    }

    private void Update()
    {
        if (!rightController.isValid || !leftController.isValid) GetDevices(!leftController.isValid, !rightController.isValid);
    }

    private void GetDevices(bool missingLeft, bool missingRight)
    {
        InputDevices.GetDevices(allDevices);
        foreach ( var device in allDevices)
        {
            if(missingLeft && device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                leftController = device;
                Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.characteristics.ToString()));
                SendHapticImpulse(true);
            }
            if(missingRight && device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                rightController = device;
                Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.characteristics.ToString()));
                SendHapticImpulse(false);
            }
        }
    }

    public void SendHapticImpulse(bool leftHand, float amplitude = 0.5f, float duration = 1.0f)
    {
        var device = leftHand ? leftController : rightController;
        if (device != null && device.TryGetHapticCapabilities(out HapticCapabilities capabilities))
        {
            if (capabilities.supportsImpulse)
            {
                uint channel = 0;
                device.SendHapticImpulse(channel, amplitude, duration);
            }
        }
    }

    ///<summary>Use higher roundings to increase precision! [rounding = 0] = raw input</summary>
    public float GetAxis(bool leftHand, XRInputAxis axis, int rounding = 2)
    {
        float output = 0f;

        if(axis.Equals(XRInputAxis.TRIGGER) || axis.Equals(XRInputAxis.GRIP))
        {
            InputFeatureUsage<float> featureUsage = axis.Equals(XRInputAxis.TRIGGER) ? CommonUsages.trigger : CommonUsages.grip;
            if (leftHand)
                leftController.TryGetFeatureValue(featureUsage, out output);
            if (!leftHand)
                rightController.TryGetFeatureValue(featureUsage, out output);

            output = (float)Math.Round(output, rounding);
        }
        else
        {
            output = GetJoystickAxis(leftHand, axis.Equals(XRInputAxis.JOYSTICK_X),rounding);
        }

        return output;
    }

    ///<summary>Use higher roundings to increase precision! [rounding = 0] = raw input</summary>
    public Vector2 GetJoystickAxis(bool leftHand, int rounding = 2)
    {
        Vector2 axis = Vector2.zero;

        if (leftHand) leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
        else rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        axis = new Vector2((float)Math.Round(axis.x, rounding), (float)Math.Round(axis.y, rounding));

        return axis;
    }

    private float GetJoystickAxis(bool leftHand, bool getX, int rounding)
    {
        Vector2 axis = GetJoystickAxis(leftHand, rounding);

        return getX ? axis.x : axis.y;
    }

    /// <summary>
    /// Use GetAxis rather then GetTrigger if you need precise information about the TriggerPress Event!
    /// </summary>
    public bool GetTrigger(bool leftHand)
    {
        bool output = false;
        if (leftHand)
            leftController.TryGetFeatureValue(CommonUsages.triggerButton, out output);
        if (!leftHand)
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out output);

        return output;
    }

    /// <summary>
    /// Use GetAxis rather then GetGrip if you need precise information about the GripPress Event!
    /// </summary>
    public bool GetGrip(bool leftHand)
    {
        bool output = false;
        if (leftHand)
            leftController.TryGetFeatureValue(CommonUsages.gripButton, out output);
        if (!leftHand)
            rightController.TryGetFeatureValue(CommonUsages.gripButton, out output);

        return output;
    }

    public bool GetButton(bool leftHand, XRInputButtons button)
    {
        bool output = false;
        InputDevice inputDevice = leftHand ? leftController : rightController;
        switch (button)
        {
            case XRInputButtons.MENU:
                if (!inputDevice.TryGetFeatureValue(CommonUsages.menuButton, out output))
                    Debug.LogError("MENU_BUTTON missing");
                break;
            case XRInputButtons.PRIMARY:
                if (!inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out output))
                    if (!inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out output))
                        if (!inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out output))
                            Debug.LogError("PRIMARY_BUTTON missing");
                            break;
            case XRInputButtons.SECONDARY:
                if (!inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out output))
                    if (!inputDevice.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out output))
                        if (!inputDevice.TryGetFeatureValue(CommonUsages.secondary2DAxisTouch, out output))
                            Debug.LogError("SECONDARY_BUTTON missing");
                break;
            default:
                break;
        }
        Debug.Log(button.ToString() + " : " + output.ToString());

        return output;
    }

    public Vector3 GetControllerVelocity(bool leftHand)
    {
        Vector3 velocity;

        if (leftHand)
            leftController.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity);
        else 
            rightController.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity);

        return velocity;
    }
}
