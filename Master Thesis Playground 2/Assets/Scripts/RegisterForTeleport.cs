using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RegisterForTeleport : MonoBehaviour, IPunInstantiateMagicCallback {

    public string[] teleportObjectNames;

    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        object[] data = info.photonView.InstantiationData;
        transform.name = (string)data[0];
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach(string s in teleportObjectNames) {
            GameObject obj = GameObject.Find(s);
            MultiuserTeleport mut = obj.GetComponent<MultiuserTeleport>();
            for (int i = 0; i < mut.nonUserTeleportObjects.Length; i++) {
                if (mut.nonUserTeleportObjects[i] == null) {
                    mut.nonUserTeleportObjects[i] = this.gameObject;
                    break;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
