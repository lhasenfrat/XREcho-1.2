using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[System.Serializable]
public class TrackedObject : System.IEquatable<TrackedObject>
{
    public GameObject obj;

    private RecordingManager recordingManager;

    public string objPath;

    public bool trackPosition;

    public bool trackRotation;

    public bool trackCamera;

    public bool trackObjInteraction;

    public bool trackUserInteraction;

    public bool trackSpaceCollision;


    public bool trackGaze;

    public float trackingRate;

    [HideInInspector]
    public float trackingInterval;

    [HideInInspector]
    public float timeSinceLastWrite;

    [HideInInspector]
    public Vector3 lastPosition;

    [HideInInspector]
    public Vector3 lastRotation;

    public GameObject replayGameObject;

    [HideInInspector]
    public string[] replayScripts;

    [HideInInspector]
    public bool hoverlocked;

    [HideInInspector]
    public bool selectlocked;

    [HideInInspector]
    public bool activatelocked;

    [HideInInspector]
    public bool lasthoverlocked;

    [HideInInspector]
    public bool lastselectlocked;

    [HideInInspector]
    public bool lastactivatelocked;

    [HideInInspector]
    public Color originalColor;

    public TrackedObject(
        GameObject gameObject,
        string objpath,
        bool position,
        bool rotation,
        bool camera,
        bool objInteraction,
        bool gaze,
        float trackingRateHz
    )
    {
        if (gameObject!=null && gameObject.activeInHierarchy) obj = gameObject;
        objPath = objpath;
        trackPosition = position;
        trackRotation = rotation;
        trackCamera = camera;
        trackObjInteraction = objInteraction;
        trackGaze = gaze;
        trackingRate = trackingRateHz;
        hoverlocked = false;
        selectlocked = false;
        activatelocked = false;
        lasthoverlocked = false;
        lastselectlocked = false;
        lastactivatelocked = false;
        if (trackObjInteraction)
            originalColor = obj.GetComponent<Renderer>().material.color;
    }

    public bool Equals(TrackedObject other)
    {
        if (!objPath.Equals(other.objPath)) return false;

        if (trackPosition != other.trackPosition) return false;

        if (trackRotation != other.trackRotation) return false;

        if (trackCamera != other.trackCamera) return false;

        if (trackObjInteraction != other.trackObjInteraction) return false;

        if (trackGaze != other.trackGaze) return false;

        return true;
    }

    public void setlisteners()
    {
        XRBaseInteractable interactable;
        recordingManager = RecordingManager.GetInstance();

        if (obj == null) return;
        if (obj.GetComponent<XRBaseInteractable>() != null)
            interactable = obj.GetComponent<XRBaseInteractable>();
        else
            return;
        if (interactable is XRGrabInteractable)
        {
            interactable.onHoverEnter.AddListener (hovered);
            interactable.onSelectEnter.AddListener (selected);
            interactable.onActivate.AddListener (activated);

            interactable.onHoverExit.AddListener (unlockHover);
            interactable.onSelectExit.AddListener (unlockSelect);
            interactable.onDeactivate.AddListener (unlockActivate);
        }
    }

    void hovered(XRBaseInteractor bi)
    {
        if (!hoverlocked)
        {
            Transform transformInteractor = bi.attachTransform;
            recordingManager.WriteInteractionAction(transformInteractor.gameObject,obj,objPath,"Hover",1);

        }
        hoverlocked = true;
        
    }

    void selected(XRBaseInteractor bi)
    {
       
        if (!selectlocked)
        {
            Transform transformInteractor = bi.attachTransform;
            recordingManager.WriteInteractionAction(transformInteractor.gameObject,obj,objPath,"Select",1);

        }
        selectlocked = true;
    }

    void activated(XRBaseInteractor bi)
    {
        
        if (!activatelocked)
        {
            Transform transformInteractor = bi.attachTransform;
            recordingManager.WriteInteractionAction(transformInteractor.gameObject,obj,objPath,"Activate",1);

        }
        activatelocked = true;
    }

    void unlockHover(XRBaseInteractor bi)
    {
        if (hoverlocked)
        {
            Transform transformInteractor = bi.attachTransform;
            recordingManager.WriteInteractionAction(transformInteractor.gameObject,obj,objPath,"Hover",0);

        }
        hoverlocked = false;
    }

    void unlockSelect(XRBaseInteractor bi)
    {
        if (selectlocked)
        {
            Transform transformInteractor = bi.attachTransform;
            recordingManager.WriteInteractionAction(transformInteractor.gameObject,obj,objPath,"Select",0);


        }
        selectlocked = false;
    }

    void unlockActivate(XRBaseInteractor bi)
    {
        if (activatelocked)
        {
            Transform transformInteractor = bi.attachTransform;
            recordingManager.WriteInteractionAction(transformInteractor.gameObject,obj,objPath,"Activate",0);
        }
        
        activatelocked = false;
    }
}
