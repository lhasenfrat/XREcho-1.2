using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class LeftHandTriggerActions : MonoBehaviour,XRIDefaultInputActions.IXRILeftHandActions
{
    XRIDefaultInputActions xriDefaultInputActions;
    private RecordingManager recordingManager;
    private Vector2 nullVector;

    private void Awake()
    {
        xriDefaultInputActions = new XRIDefaultInputActions();
        xriDefaultInputActions.XRILeftHand.SetCallbacks(this);

    }

    private void Start()
    {
        recordingManager = RecordingManager.GetInstance();
        nullVector = new Vector2();
    }

    public void OnPosition(InputAction.CallbackContext context)
    {
    }
    public void OnRotation(InputAction.CallbackContext context)
    {
    }
    public void OnTrackingState(InputAction.CallbackContext context)
    {
    }
    public void OnSelect(InputAction.CallbackContext context)
    {
        //if (!context.started)
        //    recordingManager.WriteAction("Left","Select touch",(int)context.ReadValue<Single>());

    }
    public void OnSelectValue(InputAction.CallbackContext context)
    {
    }
    public void OnActivate(InputAction.CallbackContext context)
    {
        //if (!context.started)
        //    recordingManager.WriteAction("Left","Activate touch",(int)context.ReadValue<Single>());
    }
    public void OnActivateValue(InputAction.CallbackContext context)
    {
    }
    public void OnUIPress(InputAction.CallbackContext context)
    {
        if (!context.started)
            recordingManager.WriteAction("Left","UI Press",(int)context.ReadValue<Single>());
    }
    public void OnUIPressValue(InputAction.CallbackContext context)
    {
    }
    public void OnHapticDevice(InputAction.CallbackContext context)
    {
    }
    public void OnTeleportSelect(InputAction.CallbackContext context)
    {
        //if (!context.started)
        //    recordingManager.WriteAction("Left","Teleport Select",context.ReadValue<Vector2>());

    }
    public void OnTeleportModeActivate(InputAction.CallbackContext context)
    {
        //if (!context.started)
        //    recordingManager.WriteAction("Left","Teleport Activate",context.ReadValue<Vector2>());
    }
    public void OnTeleportModeCancel(InputAction.CallbackContext context)
    {
        //if (!context.started)
        //    recordingManager.WriteAction("Left","Teleport Cancel",(int)context.ReadValue<Single>());
    }
    public void OnTurn(InputAction.CallbackContext context)
    {
        if (!context.started)
        {
            Vector2 turnVector = context.ReadValue<Vector2>();
            if (turnVector!=nullVector)
            {
                if (turnVector.x==-1)
                    recordingManager.WriteAction("Left","Turn",1,"left");   
                else if (turnVector.x==1)
                    recordingManager.WriteAction("Left","Turn",1,"right");   
            }
            else if (turnVector==nullVector)
                recordingManager.WriteAction("Left","Turn",0,0);            
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.started)
        {
            Vector2 moveVector = context.ReadValue<Vector2>();
            if (moveVector!=nullVector)
                recordingManager.WriteAction("Left","Move",1,moveVector.x,moveVector.y);
            else if (moveVector==nullVector)
                recordingManager.WriteAction("Left","Move",0,0,0);     
        }
    }
    public void OnRotateAnchor(InputAction.CallbackContext context)
    {
    }
    public void OnTranslateAnchor(InputAction.CallbackContext context)
    {
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
