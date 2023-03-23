using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LaserTarget : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    public string Identifier;
    private Transform handTransform;
    private LineRenderer laser;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (laser != null && handTransform.position != null)
        {
            laser.SetPosition(0, handTransform.position);
            laser.SetPosition(1, gameObject.transform.position);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {

        object[] instantiationData = info.photonView.InstantiationData;
        Identifier = instantiationData[0].ToString();
        handTransform = GameObject.Find(Identifier).transform.Find("SimpleQuestCharacter").Find("Right");
        laser = GetComponent<LineRenderer>();
        Vector3 color = (Vector3)instantiationData[1];
        Color laserColor = new Color(color.x, color.y, color.z);
        laser.material.color = laserColor;
        laser.material.SetColor("_EmissionColor", laserColor);
        gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = laserColor;
        gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", laserColor);

    }
}
