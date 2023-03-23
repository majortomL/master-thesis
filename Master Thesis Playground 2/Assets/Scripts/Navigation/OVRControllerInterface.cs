using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Photon.Pun;
using System;

using VPattern = VibrationPattern;

public class OVRControllerInterface : MonoBehaviour, VibrationController
{
    public EventTrigger.TriggerEvent ADown;
    public EventTrigger.TriggerEvent AUp;
    public EventTrigger.TriggerEvent BDown;
    public EventTrigger.TriggerEvent BUp;
    public EventTrigger.TriggerEvent TriggerDown;
    public EventTrigger.TriggerEvent TriggerUp;
    public EventTrigger.TriggerEvent ControllerActivate;

    //OVRInput.Controller localCtr;
    bool connectedStatus = false;

    Dictionary<VPattern, float> registeredPatterns = new Dictionary<VPattern, float>();

    VPattern testPattern;

    public GameObject grabbable;
    public GameObject grabbedObject;

    private Vector3 lastFramePosition;
    public List<GameObject> grabbables;
    private List<Vector3> positions = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        //localCtr = gameObject.GetComponent<OVRControllerHelper>().m_controller;

        testPattern = VPattern.makeUnrestricted(1, 1, 0.2f, 1);

        VibrationHandler.setImpl(this);
        grabbables = new List<GameObject>(GameObject.FindGameObjectsWithTag("Grabbable"));
    }

    // Update is called once per frame
    void Update()
    {
        //if (OVRInput.GetDown(OVRInput.Button.One, localCtr) || Input.GetKeyDown(KeyCode.Q))
        //{
        //    ADown.Invoke(new BaseEventData(EventSystem.current));
        //    //NavigationEvents.teleportButtonPressed.Invoke(transform);   // TODO find a way to use navigation events in a configurable way
        //    //startPattern(testPattern);
        //}

        //if (OVRInput.GetUp(OVRInput.Button.One, localCtr) || Input.GetKeyUp(KeyCode.Q))
        //{
        //    AUp.Invoke(new BaseEventData(EventSystem.current));
        //    //stopPattern(testPattern);
        //}

        //if (OVRInput.GetDown(OVRInput.Button.Two, localCtr) || Input.GetKeyDown(KeyCode.W))
        //{
        //    BDown.Invoke(new BaseEventData(EventSystem.current));
        //}

        //if (OVRInput.GetUp(OVRInput.Button.Two, localCtr) || Input.GetKeyUp(KeyCode.W))
        //{
        //    BUp.Invoke(new BaseEventData(EventSystem.current));
        //    //vibrate(0.5f, 0.5f, 2.0f);
        //}

        //if (OVRInput.GetDown(OVRInput.Button.Start, localCtr) || Input.GetKeyDown(KeyCode.E))
        //{
        //    startPattern(testPattern);
        //}

        //if (OVRInput.GetUp(OVRInput.Button.Start, localCtr) || Input.GetKeyUp(KeyCode.E))
        //{
        //    stopPattern(testPattern);
        //}

        //if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, localCtr) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, localCtr)) // Trigger Button was pressed
        //{
        //    //InterCubeTeleport.teleportButtonPressed(gameObject.transform);
        //    TriggerDown.Invoke(new BaseEventData(EventSystem.current));
        //}

        //if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, localCtr) || OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger, localCtr)) // Trigger was released
        //{
        //    //InterCubeTeleport.teleportButtonReleased();
        //    TriggerUp.Invoke(new BaseEventData(EventSystem.current));
        //}

        //bool contConnStatus = OVRInput.IsControllerConnected(localCtr);
        //if (connectedStatus != contConnStatus && contConnStatus)
        //{
        //    ControllerActivate.Invoke(new BaseEventData(EventSystem.current));
        //}
        //connectedStatus = contConnStatus;

        handleVibrations();

        checkInsideGrabbable();

        updatePositionsList();
    }

    void handleVibrations()
    {
        float maxFreq = 0;
        float ampSum = 0;
        List<VPattern> outdatedPattern = new List<VPattern>();
        foreach (var item in registeredPatterns)
        {
            VPattern vp = item.Key;
            float startTimestamp = item.Value;
            int iteration = (int)Mathf.Floor((Time.time - startTimestamp) / vp.cycleDuration);

            // check pattern termination requirements
            if ((vp.patternDuration > 0 && startTimestamp + vp.patternDuration < Time.time)
            || (vp.cycleCount >= 0 && iteration >= vp.cycleCount))
            {
                outdatedPattern.Add(vp);
                continue;
            }

            float normCycleTime = ((Time.time - startTimestamp) % vp.cycleDuration) / vp.cycleDuration;
            if (normCycleTime < (1.0f - vp.pauseRatio))
            {    // if in vibration phase
                float normPhaseTime = normCycleTime / (1.0f - vp.pauseRatio);           // time progression in phase [0,1]
                float normVibeTime = (normPhaseTime * vp.vibrationsPerCycle) % 1.0f;    // time progression per vibration [0,1]
                float foldedVibeTime = Mathf.Abs(normVibeTime - 0.5f) * 2.0f;           // this maps 0.5 to 0; but 0 and 1 to 1; => easy to place vibration in the middle of the time interval
                if (foldedVibeTime < vp.vibrateRatio)
                {  // if currently vibrating (otherwise there would be a pause between vibrations)
                    maxFreq = Mathf.Max(maxFreq, vp.frequency);
                    ampSum += vp.amplitude;
                }
            }
        }

        // remove old patterns
        foreach (VPattern p in outdatedPattern)
        {
            stopPattern(p);
        }

        //OVRInput.SetControllerVibration(Mathf.Min(maxFreq, 1.0f), Mathf.Min(ampSum, 1.0f), OVRInput.Controller.LTouch);
        //OVRInput.SetControllerVibration(Mathf.Min(maxFreq, 1.0f), Mathf.Min(ampSum, 1.0f), OVRInput.Controller.RTouch);
    }

    public void vibrate(float frequency, float amplitude, float duration)
    {
        startPattern(VPattern.makeTimed(amplitude, frequency, duration, duration, 1, 1));
    }

    void startVibrating(float frequency, float amplitude)
    {
        //OVRInput.SetControllerVibration(frequency, amplitude);
    }

    void stopVibrating()
    {
        //OVRInput.SetControllerVibration(0, 0);
    }


    public void startPattern(VPattern vp)
    {
        if (registeredPatterns.ContainsKey(vp))
        {
            registeredPatterns[vp] = Time.time;
        }
        else
        {
            registeredPatterns.Add(vp, Time.time);
        }
    }

    public void stopPattern(VPattern vp)
    {
        registeredPatterns.Remove(vp);
    }

    private void checkInsideGrabbable()
    {
        foreach (GameObject grab in grabbables)
        {
            BoxCollider[] colliders = grab.GetComponents<BoxCollider>();

            foreach (BoxCollider collider in colliders)
            {
                if (collider.bounds.Contains(GetComponent<SphereCollider>().bounds.center))
                {
                    grabbable = grab;
                    return;
                }
                else
                {
                    grabbable = null;
                }
            }
        }
    }

    public void tryGrab()
    {
        if (grabbable != null)
        {
            grabbedObject = grabbable;
            grabbable.transform.SetParent(transform);
            grabbable.GetComponent<Rigidbody>().isKinematic = true;
            if (grabbedObject.GetComponent<PhotonView>().OwnerActorNr != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                grabbedObject.GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer.ActorNumber);
            }
            
            //if (grabbedObject.name.Contains("Blueprint"))
            if(grabbedObject.TryGetComponent<Blueprint>(out Blueprint bp))
            {
                bp.takeoverOwnership();
            }
        }
    }

    public void tryLetGo()
    {
        if (grabbedObject != null)
        {
            grabbedObject.transform.SetParent(null);
            grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
            //grabbedObject.GetComponent<Rigidbody>().velocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            grabbedObject = null;
            grabbable = null;
        }
    }

    private void updatePositionsList()
    {
        positions.Add(transform.position);
        if (positions.Count > 15)
        {
            positions.RemoveRange(0, positions.Count - 15);
        }
    }
}
