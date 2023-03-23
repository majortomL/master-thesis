using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRGrabbable : VRInteractable
{
    public override void VRInteractionBegin()
    {
        Debug.Log("Grabbing began");

        transform.parent = interactingOwner;

    }

    public override void VRInteractionEnd()
    {
        interactingOwner = null;
        transform.parent = null;
    }
}
