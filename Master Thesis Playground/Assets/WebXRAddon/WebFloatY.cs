using Zinnia.Action;
using WebXR;
using Unity;

public class WebFloatY : FloatAction
{
    public WebXRController controller;

    private float yAxis;

    // Update is called once per frame
    void Update()
    {
        // TODO: Update this to also use the track pad of HTC
        var vector2 = controller.GetAxis2D(WebXRController.Axis2DTypes.Thumbstick);
        yAxis = vector2.y;
        Receive(yAxis);
    }
}

