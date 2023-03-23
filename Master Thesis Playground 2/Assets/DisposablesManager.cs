using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DisposablesManager : MonoBehaviour
{
    public GameObject[] Disposables;

    public List<GameObject> Anchors = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject disposable in Disposables)
        {
            foreach (Transform child in disposable.transform)
            {
                if (child.name.Contains("Anchor"))
                {
                    Anchors.Add(child.gameObject);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    private void FixedUpdate()
    {
        
    }

    void checkSnap(GameObject anchor1, GameObject anchor2)
    {

    }
}
