using Unity.VisualScripting;
using UnityEngine;
//using UnityEngine.XR;
using WebXR;

public class GrabAndThrowController : MonoBehaviour

{
    //public XRNode xrNode = XRNode.RightHand;

    public Rigidbody controllerRigidBody;
    //private InputDevice controller;
    private WebXRController controller;
    private float triggerValue = 0;
    private Rigidbody touchedObject = null;
    private bool pickedUpObject = false;
    private Vector3 positionLastFrame;
    private Vector3 velocity;

    void Start()
    {
        positionLastFrame = transform.position;
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
        if (pickedUpObject)
        {
            RecordVelocity();
        }
    }


    void OnTriggerEnter(Collider other)
    {
        // only pick up an object if user first touches it and then presses the trigger
        if (triggerValue > 0.1f) return;

        if (!other.gameObject.CompareTag("PickUp")) return;
        
        touchedObject = other.gameObject.GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerValue > 0) return;

        if (!other.gameObject.CompareTag("PickUp")) return;

        touchedObject = null;
    }

    //void GetController()
    //{ 
    //    controller = InputDevices.GetDeviceAtXRNode(xrNode);
    //}


    void HandleTriggerButton()
    {
        //if (!controller.isValid) return;

        //controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
        float triggerValue = controller.GetAxis(WebXRController.AxisTypes.Trigger);

        // only activate over a threshold, because slightly trigger might cause a press
        if (triggerValue > 0.3f)
        {
            PickUpTouchedObject();
        }
        else
        {
            if (!pickedUpObject) return;
            ReleaseTouchedObject();
        }
    }

    void PickUpTouchedObject()
    {
        if (touchedObject == null) return;

        AddParentTransform(controllerRigidBody, touchedObject);
        pickedUpObject = true;
    }

    void ReleaseTouchedObject()
    {
        if (touchedObject == null) return;

        RemoveParentTransform(touchedObject);
        AddControllerVelocity(touchedObject);
        touchedObject = null;
        pickedUpObject = false;
    }
    
    void AddParentTransform(Rigidbody parent, Rigidbody child)
    {
        // bind the touched object to the controller so they are moved together
        child.isKinematic = true;
        child.useGravity = false;
        child.transform.parent = parent.transform;
    }

    void RemoveParentTransform(Rigidbody child)
    {
        child.isKinematic = false;
        child.useGravity = true;
        child.transform.parent = null;
    }

    void AddControllerVelocity(Rigidbody throwable)
    {
        throwable.AddForce(velocity * 1.5f, ForceMode.Impulse);
    }

    void RecordVelocity()
    {
        velocity = (transform.position - positionLastFrame) / Time.deltaTime;
        positionLastFrame = transform.position;
    }
}
