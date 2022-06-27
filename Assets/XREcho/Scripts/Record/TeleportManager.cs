using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    private static TeleportManager instance;

    GameObject trackedObject;
    Vector3 lastPosition;

    public float teleportationThreshold; 

    RecordingManager recordingManager;
    
    void Awake()
    {
        if (instance)
            Debug.LogError("2 teleport manager: singleton design pattern broken");

        instance = this;
    }

    void Start()
    {
        recordingManager = RecordingManager.GetInstance();
    }

   

    void FixedUpdate()
    {
        if (trackedObject!=null)
        {
            if(Vector3.Distance(lastPosition,trackedObject.transform.position)>teleportationThreshold)
            {
                recordingManager.WriteAction(trackedObject.name,"Teleport",lastPosition,trackedObject.transform.position);
            }
            lastPosition=trackedObject.transform.position;
        }
    }

    public void Setup(GameObject gameObject)
    {
        trackedObject=gameObject;
        lastPosition=trackedObject.transform.position;
    }

    public static TeleportManager GetInstance()
    {
        return instance;
    }
}
