using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingHandler : MonoBehaviour
{
    void Awake()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            transform.localPosition = new Vector3(0, 0, 0);
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            Debug.Log("Detected webGL Player");
        } else
        {
            transform.localPosition = new Vector3(0, -0.02f, 0.05f);
            transform.localRotation = Quaternion.Euler(-55, 180, -5);
            Debug.Log("Detected Editor Player");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
