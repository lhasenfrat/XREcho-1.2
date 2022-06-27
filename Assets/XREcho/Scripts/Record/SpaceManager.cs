using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceManager : MonoBehaviour
{
    private static SpaceManager instance;

    List<TrackedObject> trackedObjects;
    List<GameObject> spaces;
    private RecordingManager recordingManager;

    IDictionary<int, int> locations;
    // Start is called before the first frame update

    void Awake()
    {
        if (instance)
            Debug.LogError("2 teleport manager: singleton design pattern broken");
        instance = this;
    }

    void Start()
    {
        recordingManager = RecordingManager.GetInstance();
        trackedObjects = recordingManager.trackedObjects;
        spaces = recordingManager.spaces;
        locations = new Dictionary<int,int>();
        for(int i =0;i<trackedObjects.Count;i++)
        {
            locations.Add(i,-1);
        }

    }

    // Update is called once per frame
    public void EnterLocation(GameObject space,GameObject collisionObject)
    {
        if (locations[trackedObjects.FindIndex(o => o.obj==collisionObject)] != spaces.FindIndex(s => s == space))
        {
            locations[trackedObjects.FindIndex(o => o.obj==collisionObject)]=spaces.FindIndex(s => s == space);
            recordingManager.WriteCollisionAction(space,collisionObject,"EnterSpace");
        }
    }
    
    public void WriteCurrentSpaces()
    {

        for(int i =0;i<trackedObjects.Count;i++)
        {
            
            if (locations[i]!=-1)
            {
                recordingManager.WriteCollisionAction(spaces[locations[i]],trackedObjects[i].obj,"EnterSpace");
            }
        }
    }

    
    public static SpaceManager GetInstance()
    {
        return instance;
    }
}
