using UnityEngine;
//using UnityEngine.XR;
using WebXR;

public class InstantiatePrefabController : MonoBehaviour
{
    //public XRNode xrNode = XRNode.RightHand;
    public GameObject prefab;

    //private InputFeatureUsage<bool> createButton = CommonUsages.primary2DAxisClick;

    private Rigidbody controllerRigidBody;
    //private InputDevice controller;
    private WebXRController controller;
    private bool lastButtonState = false;
    
    private  GameObject newGameObject = null;
    private Rigidbody newObjectRidigBody = null;

    void Start()
    {
        controllerRigidBody = GetComponent<Rigidbody>();
        controller = GetComponent<WebXRController>();
    }
    
    void FixedUpdate()
    {
        //if (!controller.isValid)
        //{
        //    // the controller can only be retrieved if it is visible
        //    GetController();
        //}
        HandleCreateButton();
    }

    //void GetController()
    //{
    //    controller = InputDevices.GetDeviceAtXRNode(xrNode);
    //    Debug.Log("Controller:" + controller.manufacturer);
    //}

    void HandleCreateButton()
    {
        //if (!controller.isValid) return;
        bool createButtonState;
        createButtonState = controller.GetButton(WebXRController.ButtonTypes.ButtonA);
        //controller.TryGetFeatureValue(createButton, out createButtonState);
        //Debug.Log(createButtonState);
        if (createButtonState != lastButtonState)
        {
            if (createButtonState)
            {
                // create object on press and bind it to controller transform
                CreateObject();
            }
            else
            {
                // release object on button release
                ReleaseCreatedObject();
            }
            lastButtonState = createButtonState;
        }
    }

    void CreateObject()
    {
        Vector3 scale = prefab.transform.localScale;
        Vector3 offset = new Vector3(0, -scale.y * 0.5f, 0);
        Vector3 position = controllerRigidBody.position + offset;
        newGameObject = Instantiate(prefab, position,  Quaternion.identity);
        newGameObject.transform.SetParent(transform);
        newObjectRidigBody = newGameObject.GetComponent<Rigidbody>();
    }
    
    void ReleaseCreatedObject()
    {
        newGameObject.transform.parent = null;
        newObjectRidigBody.isKinematic = false;
        newObjectRidigBody.useGravity = true;
        newGameObject = null;
    }
}
