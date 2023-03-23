using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CheckCollision : MonoBehaviour
{
    public bool CollidingWithOtherObject = false;
    public string OtherObjectName = "Columns";

    private void OnDisable()
    {
        CollidingWithOtherObject = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        try
        {
            if (other.transform.parent.name == OtherObjectName)
            {
                CollidingWithOtherObject = true;
            }
        }
        catch (System.NullReferenceException e)
        {

        }
    }

    private void OnTriggerExit(Collider other)
    {
        try
        {
            if (other.transform.parent.name == OtherObjectName)
            {
                CollidingWithOtherObject = false;
            }
        }
        catch (System.NullReferenceException)
        {

        }
    }
}
