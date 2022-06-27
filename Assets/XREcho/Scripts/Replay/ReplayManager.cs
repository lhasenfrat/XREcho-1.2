using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// The <c>ReplayManager</c> class contains all methods to replay the data logged by the RecordingManager.
/// </summary>
[ExecuteInEditMode]
public class ReplayManager : MonoBehaviour
{
    static ReplayManager instance;

    private bool replaying;

    public bool IsReplaying()
    {
        return replaying;
    }

    private bool paused;

    public bool IsPaused()
    {
        return paused;
    }

    private float timeMultiplier;

    public void FastForward(float _timeMultiplier)
    {
        timeMultiplier = _timeMultiplier;
    }

    private float jumpForwardBy;

    public void JumpForwardBy(float _jumpForwardBy)
    {
        jumpForwardBy = _jumpForwardBy;
    }

    private float timeSinceStartOfReplay;

    public float GetCurrentReplayTime()
    {
        return timeSinceStartOfReplay;
    }

    private float totalReplayTime;

    public float GetTotalReplayTime()
    {
        return totalReplayTime;
    }

    public string replayProject;

    private int currentRecordingIndex;
    public string replaySession;

    public string replayRecord;

    private int targetDisplay;

    private Camera mainCamera;

    public void SetMainCamera(Camera camera)
    {
        mainCamera = camera;
    }

    private Camera replayCamera;

    private bool shouldChangeCamera;

    private bool objInteractionVisu;

    private bool augmentData;

    private int nbRecordings;

    public int GetNbRecording()
    {
        return nbRecordings;
    }

    private List<int> objectsIndices;

    private List<int> actionsIndices;

    private List<int> eventsIndices;

    [HideInInspector]
    public List<List<Dictionary<string, object>>> objectsData;

    [HideInInspector]
    public List<List<Dictionary<string, object>>> actionsData;

    private List<List<TrackedObject>> trackedObjects;

    private List<List<TrackedAction>> trackedActions;

    private List<List<Dictionary<string, object>>> eventsData;

    public List<List<string>> events;

    private List<List<GameObject>> clones;

    private List<string> timestampList;
    private GameObject cloneObject;

    private List<GameObject> cloneByRecordingObjects;

    public bool cloneEverything = false;

    public float clonesTransparency = 0.5f;

    [Header("Default Replay Models")]
    public GameObject cameraModel;

    public GameObject controllerModel;

    public GameObject rightControllerModel;

    public GameObject eyeModel;

    public GameObject notFoundModel;

    [Space(8)]
    public KeyCode playPauseShortcut = KeyCode.P;

    public KeyCode stopShortcut = KeyCode.S;

    private float recAlpha = 1.0f;

    private bool keepReplayingOnNewScene;

    private bool replayingOnSceneUnloaded;

    private XREchoConfig config;

    private RecordingManager recordingManager;

    private System.Diagnostics.Stopwatch stopWatch;

    CSVWriter gazeWriter;


    private void Awake()
    {
        if (instance)
            Debug
                .LogError("2 Replay Managers: singleton design pattern broken");

        instance = this;
    }

    private void Start()
    {
        stopWatch = new System.Diagnostics.Stopwatch();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        currentRecordingIndex=-1;
        recordingManager = RecordingManager.GetInstance();
        config = XREchoConfig.GetInstance();
        gazeWriter = new CSVWriter("nullGazeData.csv");

        
        replaying = false;
        replayingOnSceneUnloaded = false;
        mainCamera = Camera.main;
        replayCamera = null;
        shouldChangeCamera = false;
        objInteractionVisu = false;
        augmentData=false;
        SetClonesTransparency();

        XREcho xrecho = XREcho.GetInstance();
        if (xrecho == null) return;

        keepReplayingOnNewScene = xrecho.dontDestroyOnLoad;

        if (
            xrecho.autoExecution == XREcho.AutoMode.AutoStartReplay &&
            !xrecho.displayGUI
        )
        {
            ChangeCamera();
            LoadAndReadLastRecord();
            StartReplaying();
        }
    }

    private void OnGUI()
    {
        if (XREcho.GetInstance().displayGUI) return;

        if (Event.current.isKey && Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == playPauseShortcut)
            {
                if (replaying)
                {
                    TogglePause();
                }
                else if (!recordingManager.IsRecording())
                {
                    LoadAndReadLastRecord();
                    StartReplaying();
                    ChangeCamera();
                }
            }
            else if (
                replaying &&
                (
                Event.current.keyCode == stopShortcut ||
                Event.current.keyCode == recordingManager.recordShortcut
                )
            )
            {
                ChangeCamera();
                StopReplaying();
            }
        }
        if (replaying)
        {
            GUILayout.BeginArea(new Rect(10, 5, 200, 100));
            recAlpha = (recAlpha + 0.01f) % 2.0f;
            float curAlpha = recAlpha > 1.0f ? 2.0f - recAlpha : recAlpha;
            GUI.color =
                paused
                    ? new Color(0.8f, 0.8f, 0.0f, 1.0f)
                    : new Color(0.0f, 0.8f, 0.0f, curAlpha);
            string lab =
                "<size=30>" + (paused ? "[Paused]" : "[Play]") + "</size>";
            GUILayout.Label (lab);
            GUI.color = Color.white;
            GUILayout.EndArea();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Reset();

        if (replayingOnSceneUnloaded)
        {
            replayingOnSceneUnloaded = false;
            ReadObjectsDataFiles(GetDataFiles("objectsdata"));
            ReadActionsDataFiles(GetDataFiles("actionsdata"));
            ReadRecordings();
            StartReplaying();
        }
    }

    private void Reset()
    {
        if (cloneObject != null) Destroy(cloneObject);

        totalReplayTime = 0;

        nbRecordings = 0;
        trackedObjects = new List<List<TrackedObject>>();
        trackedActions = new List<List<TrackedAction>>();
        timestampList = new List<string>();
        eventsData = new List<List<Dictionary<string, object>>>();
        events = new List<List<string>>();

        clones = new List<List<GameObject>>();
    }

    public void ChangeCamera()
    {
        if (replayCamera == null)
        {
            shouldChangeCamera = true;
            return;
        }

        int tmp;
        tmp = mainCamera.targetDisplay;
        mainCamera.targetDisplay = replayCamera.targetDisplay;
        replayCamera.targetDisplay = tmp;
    }

    private void OnSceneUnloaded(Scene current)
    {
        if (replaying)
        {
            replayingOnSceneUnloaded = true;
            StopReplaying();
            Reset();
        }
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        SetClonesTransparency();

        cameraModel = InitDefaultReplayModel(cameraModel, "DefaultCameraModel");
        controllerModel =
            InitDefaultReplayModel(controllerModel, "DefaultControllerModel");
        rightControllerModel =
            InitDefaultReplayModel(rightControllerModel,
            "DefaultRightControllerModel");
        eyeModel = InitDefaultReplayModel(eyeModel, "DefaultEyeModel");
        notFoundModel = InitDefaultReplayModel(notFoundModel, "NotFoundModel");
    }

    private GameObject
    InitDefaultReplayModel(GameObject model, string defaultName)
    {
        if (model != null) return model;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.name == "XREcho" + defaultName)
                return null;
        }
        GameObject prefab =
            (GameObject)
            AssetDatabase
                .LoadAssetAtPath("Assets/XREcho/Prefabs/DefaultReplayModels/" +
                defaultName +
                ".prefab",
                typeof (GameObject));
        GameObject defaultObject = (GameObject) Instantiate(prefab, transform);
        defaultObject.name = "XREcho" + defaultName;
        return defaultObject;
    }
#endif


    public static ReplayManager GetInstance()
    {
        return instance;
    }

    
    public List<string> GetDataFiles(string datatype)
    {
        return GetDataFiles(config.GetSessionFolder(),datatype);
    }

    public List<string> GetDataFiles(string folderName,string datatype)
    {
         DirectoryInfo dir;
        FileInfo[] dataFilesInfo;
        List<string> dataFiles = new List<string>();

        try
        {
            dir = new DirectoryInfo(folderName);
            dataFilesInfo =
                dir.GetFiles("*"+datatype + config.GetFileSuffix() + ".csv");
        }
        catch
        {
            dataFilesInfo = new FileInfo[] { };
            Debug.LogError("Directory " + folderName + " was not found");
        }

        foreach (FileInfo f in dataFilesInfo)
        dataFiles.Add(f.Name);

        return dataFiles;
    }

    private string
    GetCorrespondingFile(
        string timestamp,
        string type,
        string folderName = ""
    )
    {
        string folder =
            folderName != "" ? folderName : config.GetSessionFolder();

        string otherFilename =
            timestamp +
            config.filenameFieldsSeparator +
            type +
            config.GetFileSuffix() +
            ".csv";

        string correspondingFile = "";
        if (File.Exists(Path.Combine(folderName, otherFilename)))
            correspondingFile = otherFilename;

        return correspondingFile;
    }

    public void StartReplaying()
    {
        Debug.Log("Replay started.");

        objectsIndices = new List<int>();
        actionsIndices = new List<int>();
        eventsIndices = new List<int>();
        for (int i = 0; i < nbRecordings; i++)
        {
            objectsIndices.Add(0);
            actionsIndices.Add(0);
            eventsIndices.Add(0);
        }

        replaying = true;
        paused = false;
        timeMultiplier = 1.0f;
        jumpForwardBy = 0;

        stopWatch.Restart();
        timeSinceStartOfReplay = 0;

        ShowClones();
    }

    private GameObject
    AddClone(
        GameObject original,
        bool makeClone = true,
        bool moveToCloneGameObject = true
    )
    {
        GameObject clone = original;
        if (makeClone) clone = GameObject.Instantiate(original);

        if (moveToCloneGameObject)
            clone.transform.parent =
                cloneByRecordingObjects[cloneByRecordingObjects.Count - 1]
                    .transform;

        clones[clones.Count - 1].Add(clone);

        return clone;
    }

    private void InstantiateClones()
    {
        if (cloneObject != null) Destroy(cloneObject);

        cloneObject = new GameObject("Clones");
        cloneObject.transform.parent = transform;
        cloneByRecordingObjects = new List<GameObject>();

        for (int i = 0; i < nbRecordings; i++)
        {
            GameObject recordingObject = new GameObject("Recording " + i);
            recordingObject.transform.parent = cloneObject.transform;
            cloneByRecordingObjects.Add (recordingObject);
            clones.Add(new List<GameObject>());

            for (int j = 0; j < trackedObjects[i].Count; j++)
            {
                TrackedObject to = trackedObjects[i][j];
                GameObject clone = null;

                if (
                    to.replayGameObject == null &&
                    !cloneEverything &&
                    to.obj != null
                )
                {
                    clone = AddClone(to.obj, false, false); // not really cloned here
                    DisableCloneComponents(clone, true);
                }
                else if (to.replayGameObject != null)
                {
                    clone = AddClone(to.replayGameObject);
                    DisableCloneComponents(clone, false);

                }
                else if (to.obj != null)
                {
                    clone = AddClone(to.obj);
                    DisableCloneComponents(clone, true);

                }
                else
                {
                    /* Obsolete?
                    Debug.Log("No game object given and original not found, creating empty GameObject for: " + to.objPath);
                    string[] objPathSplit = to.objPath.Split('/');
                    string objName = objPathSplit[objPathSplit.Length - 1];
                    clone = AddClone(new GameObject(objName), false, true);
                    */
                }
                to.obj=clone;
            }
        }
    }

    private void MakeClonesTransparent()
    {
        for (int i = 0; i < nbRecordings; i++)
        foreach (GameObject clone in clones[i])
        foreach (MeshRenderer
            meshRenderer
            in
            clone.GetComponentsInChildren<MeshRenderer>()
        )
        meshRenderer.gameObject.AddComponent<MeshTransparencyHandler>();

        SetClonesTransparency();
    }

    private void SetClonesTransparency()
    {
        if (clones == null) return;

        for (int i = 0; i < nbRecordings; i++)
        foreach (GameObject clone in clones[i])
        foreach (MeshTransparencyHandler
            transparencyHandler
            in
            clone.GetComponentsInChildren<MeshTransparencyHandler>()
        )
        transparencyHandler.SetTransparencyAlpha(clonesTransparency);
    }

    private void DisableCloneComponents(GameObject clone, bool disableScripts)
    {
        if (disableScripts)
            foreach (MonoBehaviour
                monoBehaviour
                in
                clone.GetComponentsInChildren<MonoBehaviour>()
            )
            if (monoBehaviour != null) monoBehaviour.enabled = false;


        //TODO voir si ca impact ailleurs de commenter ça pour les raycast
        //foreach (Collider collider in clone.GetComponentsInChildren<Collider>())
        //if (collider != null) collider.enabled = false;

        foreach (Rigidbody
            rigidbody
            in
            clone.GetComponentsInChildren<Rigidbody>()
        )
        {
            if (rigidbody != null)
            {
                rigidbody.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousSpeculative;
                rigidbody.isKinematic = true;
            }
        }
    }

    private void DisableClonesComponents()
    {
        for (int i = 0; i < nbRecordings; i++)
        {
            foreach (GameObject clone in clones[i])
            {
                DisableCloneComponents(clone, true);
            }
        }
    }

    private void AddReplayScripts()
    {
        for (int i = 0; i < nbRecordings; i++)
        foreach (TrackedObject to in trackedObjects[i])
        {
            if (to.replayScripts == null) continue;
            foreach (string replayScript in to.replayScripts)
            {
                if (!to.obj.GetComponent((Type.GetType(replayScript))))
                    to.obj.AddComponent(Type.GetType(replayScript));
            }
        }
    }

    private void ShowClones()
    {
        if (cloneObject != null) cloneObject.SetActive(true);
    }

    private void HideClones()
    {
        if (cloneObject != null) cloneObject.SetActive(false);
    }

    private void DestroyClones()
    {
        if (!cloneEverything) return;

        for (int i = 0; i < nbRecordings; i++)
        foreach (GameObject clone in clones[i]) Destroy(clone);
    }

    public void StopReplaying()
    {
        stopWatch.Stop();
        double systemSecondsElapsed = stopWatch.Elapsed.TotalSeconds;

        //Debug.Log("System seconds elapsed = " + systemSecondsElapsed + ", unity accumulated seconds of replaying = " + timeSinceStartOfReplay + " and difference = " + (timeSinceStartOfReplay - (float)systemSecondsElapsed));
        Debug.Log("Replay stopped.");
        replaying = false;
        replayCamera = null;
        HideClones();
    }

    public void TogglePause()
    {
        paused = !paused;
    }

    private void LoadAndReadLastRecord()
    {
        List<string> curto = GetDataFiles("objectsdata");
        List<string> toRead = new List<string> { curto[curto.Count - 1] };

        
        List<string> curta = GetDataFiles("actionsdata");
        List<string> taRead = new List<string> { curta[curta.Count - 1] };

        if (curto.Count == 0 || curta.Count==0)
        {
            Debug.LogError("Last record not found, replay aborted.");
            return;
        }
        Debug.Log("Loading replay " + toRead[0] + "...");

        ReadObjectsDataFiles (toRead);
        ReadActionsDataFiles(taRead);
        ReadRecordings (toRead);
    }

    public void ReadObjectsDataFile(string folderName, int recordingId)
    {
        List<string> objectsDataFiles = GetDataFiles(folderName,"objectsdata");
        if (recordingId < 0 || recordingId >= objectsDataFiles.Count)
        {
            Debug
                .LogError("Error: no record file number " +
                recordingId +
                " found, aborting reading");
            return;
        }
        ReadObjectsDataFiles(new List<string> { objectsDataFiles[recordingId] },
        folderName);
    }

    public void ReadActionsDataFile(string folderName, int recordingId)
    {
        List<string> actionsDataFiles = GetDataFiles(folderName,"actionsdata");
        if (recordingId < 0 || recordingId >= actionsDataFiles.Count)
        {
            Debug
                .LogError("Error: no record file number " +
                recordingId +
                " found, aborting reading");
            return;
        }
        ReadActionsDataFiles(new List<string> { actionsDataFiles[recordingId] },
        folderName);
    }

    // out of readRecording (to be done asynchrously) : should be called before readRecording !
    public void ReadObjectsDataFiles(
        List<string> objectsDataFiles,
        string folderName = ""
    )
    {
        objectsData = new List<List<Dictionary<string, object>>>();

        string folder =
            folderName != "" ? folderName : config.GetSessionFolder();
        foreach (string objectsDataFile in objectsDataFiles)
        {
            List<Dictionary<string, object>> data =
                CSVReader.ReadCSV(Path.Combine(folder, objectsDataFile));
            objectsData.Add (data);
        }
    }

    public void ReadActionsDataFiles(
        List<string> actionsDataFiles,
        string folderName = ""
    )
    {
        actionsData = new List<List<Dictionary<string, object>>>();

        string folder =
            folderName != "" ? folderName : config.GetSessionFolder();
        foreach (string actionsDataFile in actionsDataFiles)
        {
            List<Dictionary<string, object>> data =
                CSVReader.ReadCSV(Path.Combine(folder, actionsDataFile));
            actionsData.Add (data);
        }
        Debug.Log("action data length : "+actionsData.Count);

    }


    public void ReadRecording(string folderName, int recordingId)
    {
        List<string> objectsDataFiles = GetDataFiles(folderName,"objectsdata");
        if (recordingId < 0 || recordingId >= objectsDataFiles.Count)
        {
            Debug
                .LogError("Error: no record file number " +
                recordingId +
                " found, aborting reading");
            return;
        }
        ReadRecordings(new List<string> { objectsDataFiles[recordingId] },
        folderName);
    }

    public void ReadRecordings()
    {
        ReadRecordings(GetDataFiles("objectsdata"));
    }

    /*
    * Reads all the recordings matching the provided list and prepares for replaying
    */
    public void ReadRecordings(
        List<string> objectsDataFiles,
        string folderName = ""
    )
    {
        Reset();
        string folder =
            folderName != "" ? folderName : config.GetSessionFolder();

        //List<string> objectsDataFiles = GetDataFiles("objectsdata");
        List<TrackedObject> trackedObjectsList;
        List<TrackedAction> trackedActionsList;
        List<TrackedData> trackedDataList;
        foreach (string objectsDataFile in objectsDataFiles)
        {

            string timestamp =  objectsDataFile.Split(config.filenameFieldsSeparator)[0];
            timestampList.Add(timestamp);
            string objectsFormatFile =
                GetCorrespondingFile(timestamp, "objectsFormat", folder);

            if (objectsFormatFile.Equals(""))
            {
                Debug
                    .LogError("Error: no corresponding objects format file for " +
                    objectsDataFile +
                    " found, aborting reading");
                return;
            }

            nbRecordings++;
            LoadObjectsFormat(Path.Combine(folder, objectsFormatFile),
            out trackedObjectsList,
            out trackedDataList);
            trackedObjects.Add (trackedObjectsList);

            LoadObjectsData(Path.Combine(folder, objectsDataFile),
            nbRecordings - 1);

            /*
             * Loading Events Format And Data Files
             */
            string eventsFormatFile =
                GetCorrespondingFile(timestamp, "eventsFormat", folder);
            string eventsDataFile =
                GetCorrespondingFile(timestamp, "eventsData", folder);

            if (eventsFormatFile.Equals("") || eventsDataFile.Equals(""))
            {
                Debug
                    .Log("No corresponding events data or format file found for " +
                    objectsDataFile);
                events.Add(null);
                eventsData.Add(null);
            }
            else
            {
                LoadEventsFormat(Path.Combine(folder, eventsFormatFile));
                LoadEventsData(Path.Combine(folder, eventsDataFile));
            }

             /*
             * Loading Actions Format And Data Files
             */
            string actionsFormatFile =
                GetCorrespondingFile(timestamp, "actionsFormat", folder);
            string actionsDataFile =
                GetCorrespondingFile(timestamp, "actionsData", folder);
            if (actionsFormatFile.Equals("") || actionsDataFile.Equals(""))
            {
                Debug
                    .Log("No corresponding actions data or format file found for " +
                    objectsDataFile);
            }
            else
            {
                LoadActionsFormat(Path.Combine(folder, actionsFormatFile),out trackedActionsList);
                LoadActionsData(Path.Combine(folder, actionsDataFile),
                nbRecordings - 1);
                trackedActions.Add(trackedActionsList);

            }
            Debug.Log("Replay loaded successfully.");
        }

        InstantiateClones();

        //AddReplayScripts();
        //MakeClonesTransparent();
        HideClones();
    }

    public void LoadObjectsFormat(
        string filepath,
        out List<TrackedObject> trackedObjects,
        out List<TrackedData> trackedData
    )
    {
        trackedObjects = new List<TrackedObject>();
        trackedData = new List<TrackedData>();
        List<Dictionary<string, object>> format = CSVReader.ReadCSV(filepath);

        ObjPathCache objPathCache = new ObjPathCache();

        foreach (Dictionary<string, object> entry in format)
        {
            int objectId = (int)(float)(entry["objectId"]);

            string objPath = (string) entry["trackedData"];
            GameObject obj = objPathCache.GetNextObject(objPath);
            bool trackPosition = bool.Parse((string) entry["position"]);
            bool trackRotation = bool.Parse((string) entry["rotation"]);
            bool trackCamera = bool.Parse((string) entry["camera"]);
            bool trackObjInteraction =
                bool.Parse((string) entry["objectInteraction"]);
            bool trackGaze = bool.Parse((string) entry["gaze"]);

            float trackingRate = (float)(entry["trackingRate"]);

            TrackedObject to =
                new TrackedObject(obj,
                    objPath,
                    trackPosition,
                    trackRotation,
                    trackCamera,
                    trackObjInteraction,
                    trackGaze,
                    trackingRate);
            trackedObjects.Add (to);

            if (entry.ContainsKey("replayScripts"))
                to.replayScripts =
                    ((string) entry["replayScripts"])
                        .Split(config.replayScriptsSeparator);

            if (entry.ContainsKey("replayGameObject"))
            {
                objPath = (string) entry["replayGameObject"];
                GameObject replayGameObject = GameObject.Find(objPath);
                to.replayGameObject = replayGameObject;
            }
            if (to.replayGameObject == null || to.replayGameObject == to.obj)
            {
                if (to.obj != null)
                {
                    if (to.obj.tag == "MainCamera")
                    {
                        to.replayGameObject = cameraModel;
                    }
                    else if (to.obj.GetComponent<XRBaseController>() != null)
                    {
                        if (to.obj.name.Contains("Right"))
                        {
                            to.replayGameObject = rightControllerModel;
                        }
                        else
                        {
                            to.replayGameObject = controllerModel;
                        }
                    }
                    else if (to.obj.name == "XREchoEyeTracker")
                    {
                        to.replayGameObject = eyeModel;
                    }
                }
                else
                {
                    string[] splited = to.objPath.Split('/');
                    string soloName = splited[splited.Length - 1];
                    if (soloName == "Main Camera")
                        to.replayGameObject = cameraModel;
                    else if (soloName == "XREchoEyeTracker")
                        to.replayGameObject = eyeModel;
                    else
                        to.replayGameObject = notFoundModel;
                }
            }
        }
    }

    private void LoadEventsFormat(string filepath)
    {
        events.Add(new List<string>());
        List<Dictionary<string, object>> format = CSVReader.ReadCSV(filepath);

        foreach (Dictionary<string, object> entry in format)
        {
            string eventName = (string) entry["event"];
            events[events.Count - 1].Add(eventName);
        }
    }

    private void LoadActionsFormat(string filepath, out List<TrackedAction> trackedActions)
    {
        trackedActions = new List<TrackedAction>();
        List<Dictionary<string, object>> format = CSVReader.ReadCSV(filepath);

        foreach (Dictionary<string, object> entry in format)
        {
            TrackedAction ta = new TrackedAction();
            ta.trackedActionName = (string) entry["ActionName"];
            int nbParameters = (int)(float) entry["NbParam"];
            trackedActions.Add(ta);
        }
    }

    /*
     * Reads a datafile
     */
    private void LoadObjectsData(string dataFilepath, int recId)
    {
        List<Dictionary<string, object>> data = objectsData[recId];

        if (data.Count != 0)
        {
            float totalReplayTimeOfObjects =
                (float) data[data.Count - 1]["timestamp"];
            totalReplayTime =
                Mathf.Max(totalReplayTime, totalReplayTimeOfObjects);
        }
    }

    private void LoadActionsData(string dataFilepath, int recId)
    {
        List<Dictionary<string, object>> data = actionsData[recId];

        if (data.Count != 0)
        {
            float totalReplayTimeOfActions =
                (float) data[data.Count - 1]["timestamp"];
            totalReplayTime =
                Mathf.Max(totalReplayTime, totalReplayTimeOfActions);
        }
    }

    private void LoadEventsData(string dataFilepath)
    {
        List<Dictionary<string, object>> data = CSVReader.ReadCSV(dataFilepath);
        eventsData.Add (data);

        if (data.Count != 0)
        {
            float totalReplayTimesOfEvents =
                (float) data[data.Count - 1]["timestamp"];
            totalReplayTime =
                Mathf.Max(totalReplayTime, totalReplayTimesOfEvents);
        }
    }

    public static List<int>
    LoadObjInteractions(Dictionary<string, object> entry)
    {
        List<int> interactions = new List<int>();

        foreach (string
            key
            in
            new string[] { "hovered", "selected", "activated" }
        )
        {
            if (entry.ContainsKey(key))
                interactions.Add((int) (float) entry[key]);
        }

        return interactions;
    }

    public static Vector3 LoadPosition(Dictionary<string, object> entry)
    {
        Vector3 position = new Vector3();
        int i = 0;

        foreach (string
            key
            in
            new string[] { "position.x", "position.y", "position.z" }
        )
        {
            if (entry.ContainsKey(key)) position[i] = (float) entry[key];
            i++;
        }

        return position;
    }

    public static Vector3 LoadRotation(Dictionary<string, object> entry)
    {
        Vector3 rotation = new Vector3();
        int i = 0;

        foreach (string
            key
            in
            new string[] { "rotation.x", "rotation.y", "rotation.z" }
        )
        {
            if (entry.ContainsKey(key)) rotation[i] = (float) entry[key];
            i++;
        }

        return rotation;
    }

    public static int LoadGazed(Dictionary<string, object> entry)    
    {
        if (entry.ContainsKey("gazed"))
            return (int)(float) entry["gazed"];
        else
            return -1;
    }
    private void Update()
    {
        if (jumpForwardBy == 0 && (!replaying || paused)) return;

        timeSinceStartOfReplay +=
            jumpForwardBy != 0
                ? jumpForwardBy
                : Time.deltaTime * timeMultiplier;

        if (jumpForwardBy < 0)
        {
            for (int i = 0; i < nbRecordings; i++)
            {
                objectsIndices[i] = 0;
                actionsIndices[i] = 0;
                eventsIndices[i] = 0;
            }
        }

        jumpForwardBy = 0;
        bool allFinishedPlaying = true;
        for (int i = 0; i < nbRecordings; i++)
        {
            if (i!=currentRecordingIndex)
                gazeWriter.Close();

            if (!ReplayData(i)) allFinishedPlaying = false;
            
        }
        if (allFinishedPlaying)
        {
            paused = true;
            
            if (!XREcho.GetInstance().displayGUI)
            {
                ChangeCamera();
                StopReplaying();
            }
        }
    }

    public void Gazing(GameObject go)
    {
        if (augmentData)
        {
            for(int i=0;i<trackedObjects[currentRecordingIndex].Count;i++)
            {
                TrackedObject to = trackedObjects[currentRecordingIndex][i];
                if (go==to.obj && to.trackGaze)
                {
                    gazeWriter.Write(timeSinceStartOfReplay.ToString(), true);
                    gazeWriter.WriteLine (i);
                }
            }
        }
    }
    private string GetAugmentationSavePath()
    {
        return Path.Combine(config.GetSessionFolder(), timestampList[currentRecordingIndex]+"gazeData.csv");
    }
    // Returns true if the replay is finished
    private bool ReplayData(int recordingIndex)
    {
        currentRecordingIndex=recordingIndex;
        if(gazeWriter==null || gazeWriter.closed==true)
            gazeWriter = new CSVWriter(GetAugmentationSavePath());
        

       
        // We read as many entries as we can until we reach a timestamp in the future
        while (true)
        {
            if (
                objectsIndices[recordingIndex] >=
                objectsData[recordingIndex].Count &&
                eventsIndices[recordingIndex] >=
                eventsData[recordingIndex].Count
            ) return true;

            float objectTimestamp = float.PositiveInfinity;
            float eventTimestamp = float.PositiveInfinity;
            float actionTimestamp = float.PositiveInfinity;
            int objectId;        
            if (
                objectsData[recordingIndex] != null &&
                objectsIndices[recordingIndex] !=
                objectsData[recordingIndex].Count
            )
                objectTimestamp =
                    (float)
                    objectsData[recordingIndex][objectsIndices[recordingIndex]]["timestamp"];

            if (
                events[recordingIndex] != null &&
                eventsIndices[recordingIndex] !=
                eventsData[recordingIndex].Count
            )
                eventTimestamp =
                    (float)
                    eventsData[recordingIndex][eventsIndices[recordingIndex]]["timestamp"];

            if (
                actionsData[recordingIndex] != null &&
                actionsIndices[recordingIndex] !=
                actionsData[recordingIndex].Count
            )
                actionTimestamp =
                    (float)
                    actionsData[recordingIndex][actionsIndices[recordingIndex]]["timestamp"];

            float minTimestamp = Mathf.Min(objectTimestamp, eventTimestamp,actionTimestamp);
            if (minTimestamp > timeSinceStartOfReplay) return false;

            // if the event happens before the object update
            if (eventTimestamp == minTimestamp)
            {
                int eventId =
                    (int)
                    (float)
                    eventsData[recordingIndex][eventsIndices[recordingIndex]]["eventId"];
                string eventName = events[recordingIndex][eventId];

                //Debug.Log("event " + eventName + " triggered !");
                eventsIndices[recordingIndex]++;

                if (eventName.Contains("SceneLoad"))
                {
                    string sceneName =
                        eventName.Split(config.sceneLoadEventSeparator)[1];

                    //WARNING : Replacing SteamVR_LoadLevel by SceneManager.LoadScene
                    SceneManager.LoadScene (sceneName);
                    replayingOnSceneUnloaded = true;
                }

                continue;
            }

            if (actionTimestamp == minTimestamp)
            {

                int actionId =
                    (int)
                    (float)
                    actionsData[recordingIndex][actionsIndices[recordingIndex]]["ActionId"];

                objectId =
                    (int)
                    (float)
                    actionsData[recordingIndex][actionsIndices[recordingIndex]]["ObjectId"];

                TrackedAction ta = trackedActions[recordingIndex][actionId];
                GameObject obj = trackedObjects[recordingIndex][objectId].obj;

                switch (ta.trackedActionName)
                {
                    case "Declare Camera":
                            Camera camera = obj.GetComponent<Camera>();
                            if (camera == null) camera = obj.AddComponent<Camera>();
                            camera.fieldOfView = (float) actionsData[recordingIndex][actionsIndices[recordingIndex]]["Param1"];
                            camera.aspect = (float) actionsData[recordingIndex][actionsIndices[recordingIndex]]["Param2"];
                            camera.nearClipPlane = (float) actionsData[recordingIndex][actionsIndices[recordingIndex]]["Param3"];
                            camera.farClipPlane = (float) actionsData[recordingIndex][actionsIndices[recordingIndex]]["Param4"];
                            replayCamera = camera;

                            if (shouldChangeCamera)
                            {
                                ChangeCamera();
                                shouldChangeCamera = false;
                            }
                        break;
                }
                actionsIndices[recordingIndex]++;

                continue;
            }

            objectId =
                (int)
                (float)
                objectsData[recordingIndex][objectsIndices[recordingIndex]]["objectId"];

            if (objectId < trackedObjects[recordingIndex].Count)
            {
                TrackedObject to = trackedObjects[recordingIndex][objectId];

                if (to.obj != null)
                {
                    to.obj.SetActive(true);
                }

                Vector3
                    position =
                        LoadPosition(objectsData[recordingIndex][objectsIndices[recordingIndex]]),
                    rotation =
                        LoadRotation(objectsData[recordingIndex][objectsIndices[recordingIndex]]),
                    emptyVector = new Vector3();

                List<int> interactions =
                    LoadObjInteractions(objectsData[recordingIndex][objectsIndices[recordingIndex]]);

                int gazed = LoadGazed(objectsData[recordingIndex][objectsIndices[recordingIndex]]);

                if (position != emptyVector)
                    to.obj.transform.position = position;
                if (rotation != emptyVector)
                    to.obj.transform.eulerAngles = rotation;

                
                    
                
                if (to.obj.GetComponent<Renderer>())
                {
                    if (objInteractionVisu && interactions.Count == 3)
                    {
                        int hover = interactions[0];
                        int select = interactions[1];
                        int activate = interactions[2];
    
                        if (activate != 0)
                            to.obj.GetComponent<Renderer>().material.color =
                                BlendColor(Color.red,to.obj.GetComponent<Renderer>().material.color);
                        if (select != 0)
                            to.obj.GetComponent<Renderer>().material.color =
                                BlendColor(new Color(1, 165f / 255, 0, 1),to.obj.GetComponent<Renderer>().material.color);
                        else if (hover != 0)
                            to.obj.GetComponent<Renderer>().material.color =
                                BlendColor(Color.yellow,to.obj.GetComponent<Renderer>().material.color);
                        else
                            to.obj.GetComponent<Renderer>().material.color =
                                to.originalColor;
                    }
                    if (gazed!=-1)
                    {
                        if(gazed==1)
                        {
                            to.obj.GetComponent<Renderer>().material.color = BlendColor(Color.blue,to.obj.GetComponent<Renderer>().material.color);
                        }
                        if(gazed==0)
                        {
                            to.obj.GetComponent<Renderer>().material.color = to.originalColor;
                        }
                    }




                }
            }
            objectsIndices[recordingIndex]++;
        }
        currentRecordingIndex=-1;
    }

    public void ActivateObjInteractionVisu()
    {
        objInteractionVisu = !objInteractionVisu;
    }
    public void setAugmentData()
    {
        augmentData = !augmentData;
    }

    private Color BlendColor(Color Color1,Color Color2)
    {
        return new Color((Color1.r*3+Color2.r)/4f,(Color1.g*3+Color2.g)/4f,(Color1.b*3+Color2.b)/4f);
    }
}
