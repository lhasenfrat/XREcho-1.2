using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordingSpace : MonoBehaviour
{
    private SpaceManager spaceManager;

    private void Start()
    {
        spaceManager = SpaceManager.GetInstance();
    }
    private void OnTriggerEnter(Collider collision)
    {
        spaceManager.EnterLocation(gameObject,collision.gameObject);
    }

    private void OnTriggerExit(Collider collision)
    {
        //spaceManager.LeaveLocation(gameObject,collision.gameObject);
    }
}
