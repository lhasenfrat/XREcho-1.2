using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class HMDTriggerActions : MonoBehaviour,XRIDefaultInputActions.IXRIHMDActions
{
    XRIDefaultInputActions xriDefaultInputActions;

    private void Awake()
    {
        xriDefaultInputActions = new XRIDefaultInputActions();
        xriDefaultInputActions.XRIHMD.SetCallbacks(this);
    }

    public void OnPosition(InputAction.CallbackContext context)
    {
        Debug.Log("position");
    }
    public void OnRotation(InputAction.CallbackContext context)
    {
        Debug.Log("rotation");
    }
    private void OnDisable()
    {
        xriDefaultInputActions.Disable();
    }
    private void OnEnable()
    {
        xriDefaultInputActions.Enable();
    }

}
