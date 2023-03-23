using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameDisplay : MonoBehaviour
{
    public Transform belongsTo;
    public float distance = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = belongsTo.position + Vector3.up * distance;
        transform.LookAt(Camera.main.transform);
        string playerName = transform.parent.parent.name;
        Text t = GetComponentInChildren<Text>();
        t.text = playerName;

        // this just disables your own name tag (side effect: if other players come too close theirs disappears as well)
        t.enabled =(Camera.main.transform.position - transform.position).magnitude > distance + 0.01;
    }
}
