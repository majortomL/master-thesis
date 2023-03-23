using UnityEngine;
using WebXR;
//using UnityEngine.XR;
using Unity.VisualScripting;
using System;

public class ScaleController : MonoBehaviour
{
    //public XRNode xrNode = XRNode.LeftHand;
    public float scaleMultiplier = 1.0f;
    public float massMultiplier = 0.2f;

    public float minMass = 0.1f;
    public float maxMass = 100.0f;

    private Rigidbody controllerRigidBody;
    //private InputDevice controller;
    private WebXRController controller;

    private bool lastTriggerState = false;
    private GameObject touchedObject;
    private Rigidbody touchedObjectRigidbody;

    private Vector3 initialControllerPosition = Vector3.zero;
    private Vector3 initialScale = Vector3.zero;
    private Vector3 initialObjectPosition = Vector3.zero;
    float initialMass = 0;

    void Start()
    {
        controller = GetComponent<WebXRController>();
        controllerRigidBody = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
        //if (!controller.isValid)
        //{
        //    // the controller can only be retrieved if it is visible
        //    GetController();
        //}
        
        HandleTriggerButton();

        if (lastTriggerState && touchedObject != null)
        {
            ScaleObject(touchedObject, touchedObjectRigidbody);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (lastTriggerState) return;

        if (!other.gameObject.CompareTag("PickUp")) return;

        touchedObject = other.gameObject;
    }

    //void GetController()
    //{
    //    controller = InputDevices.GetDeviceAtXRNode(xrNode);
    //}

    void HandleTriggerButton()
    {
        //if (!controller.isValid) return;
        bool currentTriggerState = TriggerPressed();

        // store the object's properties once on press
        // release the object on trigger release
        if (currentTriggerState != lastTriggerState)
        {
            if (currentTriggerState)
            {
                StoreInitialTransforms();
            }
            else
            {
                touchedObject = null;
            }
            lastTriggerState = currentTriggerState;
        }
    }

    bool TriggerPressed()
    {
        float triggerButtonValue = 0;
        triggerButtonValue = controller.GetAxis(WebXRController.AxisTypes.Trigger);
        //controller.TryGetFeatureValue(CommonUsages.trigger, out triggerButtonValue);
        return triggerButtonValue > 0.1f;
    }

    void StoreInitialTransforms()
    {
        // get the transform position and scale from the start of the scale process
        initialControllerPosition = controllerRigidBody.position;
        if (touchedObject != null)
        {
            touchedObjectRigidbody = touchedObject.GetComponent<Rigidbody>();
            initialScale = touchedObject.transform.localScale;
            initialObjectPosition = touchedObject.transform.position;
            initialMass = touchedObjectRigidbody.mass;
        }
    }

    void ScaleObject(GameObject objectToScale, Rigidbody objectToScaleRigidbody)
    {
        if (objectToScale == null) return;
        float scaleFactor = CalculateScaleFactor(objectToScale);

        objectToScale.transform.localScale = CalculateNewScale(scaleFactor);;
        objectToScaleRigidbody.mass = CalculateNewMass(scaleFactor);
    }
    
    float CalculateScaleFactor(GameObject objectToScale)
    {
        // calculates the scale factor based on the position of the assigned controller for scaling
        // based on the distance between the controller and object at the start of the scale process (= trigger was pressed)
        Vector3 controllerPosition = controllerRigidBody.transform.position;
        Vector3 objectPosition = objectToScale.transform.position;
        float currentDistance = Vector3.Distance(controllerPosition, objectPosition);
        float initialDistance = Vector3.Distance(initialControllerPosition, initialObjectPosition);
        float scaleFactor = (currentDistance / initialDistance);
        return scaleFactor;
    }
    
    Vector3 CalculateNewScale(float scaleFactor)
    {
        float multipliedScaleFactor = scaleFactor * scaleMultiplier;
        Vector3 newScale = initialScale * multipliedScaleFactor;

        return newScale;
    }

    float CalculateNewMass(float scaleFactor)
    {
        float newMass = initialMass * scaleFactor * massMultiplier;

        newMass = Math.Min(newMass, maxMass);
        newMass = Math.Max(newMass, minMass);

        return newMass;
    }


}
