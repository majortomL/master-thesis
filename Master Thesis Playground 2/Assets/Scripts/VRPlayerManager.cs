using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class VRPlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    private string playerName = "";

    public static GameObject LocalPlayerInstance;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Would need to be observed by a PhotonView on the prefab

        if (stream.IsWriting)
        {
            // We own this player: send the others our data
         //   stream.SendNext(Object obj); // can observe any variable
        }
        else
        {
            // Network player, receive data
         //   this.obj = (Object)stream.ReceiveNext(); // receive the same variable from the stream if not local player
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
        playerName = gameObject.GetComponent<PhotonView>().Owner.NickName;
        gameObject.name = playerName;

    }

    void Awake()
    {
        if (photonView.IsMine)
        {
            VRPlayerManager.LocalPlayerInstance = this.gameObject;
        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity on every frame.
    /// </summary>
    void Update()
    {
        /*
        if (photonView.IsMine)
        {
            ProcessInputs();
        }
        */

    }





    /// <summary>
    /// Processes the inputs.
    /// </summary>
    void ProcessInputs()
    {
        
    }
}
