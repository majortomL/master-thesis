using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRDraggable : VRInteractable
{
    public override void VRInteractionBegin()
    {
        Debug.Log("Dragging began");

        transform.parent = interactingOwner;

    }

    public override void VRInteractionEnd()
    {
        interactingOwner = null;
        transform.parent = null;
    }
}
