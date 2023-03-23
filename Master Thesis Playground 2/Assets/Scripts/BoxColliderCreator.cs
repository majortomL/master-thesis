﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxColliderCreator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.AddComponent<BoxCollider>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
