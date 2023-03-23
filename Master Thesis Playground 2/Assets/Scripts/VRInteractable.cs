using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;


public class VRInteractable : MonoBehaviourPun, IPunObservable
{
    private string objectName = "";

    public static int localPlayerID;
    public int masterClientID;

    public string playerTag = "Player";

    public Transform interactingOwner;


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
        objectName = gameObject.name;

        if (objectName.EndsWith("(Clone)"))
            objectName = objectName.Replace("(Clone)", "");
        gameObject.name = objectName;


        // find local player

        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag(playerTag);

        for (int i = 0; i < playerObjs.Length; i++)
        {
            PhotonView ppv = playerObjs[i].GetComponent<PhotonView>();

            if (ppv.Owner.IsMasterClient)
            {
                masterClientID = ppv.Owner.ActorNumber;
            }

            if (ppv.IsMine)
            {
                localPlayerID = ppv.Owner.ActorNumber;

            }
        }

        Debug.Log(localPlayerID);
    }

    // Update is called once per frame



    [PunRPC]
    public void SetOwner(int newOwnerID)
    {
        if (photonView.IsMine)
        {
            Debug.Log("owner");

            GameObject[] playerObjs = GameObject.FindGameObjectsWithTag(playerTag);

            for (int i = 0; i < playerObjs.Length; i++)
            {
                Player pl = playerObjs[i].GetComponent<PhotonView>().Owner;

                if (pl.ActorNumber.Equals(newOwnerID))
                {
                    photonView.TransferOwnership(pl);

                    break;
                }
            }
        }

    }


    public void TakeoverOwnership()
    {
        this.photonView.RPC("SetOwner", RpcTarget.All, localPlayerID);
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Right") || other.gameObject.transform.tag.Equals("Left"))
        {
            if (other.gameObject.GetComponent<VRInteractionInput>().isInteracting && !photonView.IsMine)
            {
                Debug.Log("colliding");

                TakeoverOwnership();

                interactingOwner = other.gameObject.transform;

                VRInteractionBegin(); // works fine in case if Dragging - the hand stays outside; in general need to solve the exact interaction timing
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Right") || other.gameObject.transform.tag.Equals("Left"))
        {
            if (other.gameObject.GetComponent<VRInteractionInput>().isInteracting && !photonView.IsMine)
            {
                TakeoverOwnership();

                interactingOwner = other.gameObject.transform;

                VRInteractionBegin(); // works fine in case if Dragging - the hand stays outside; in general need to solve the exact interaction timing
            }
            if (!other.gameObject.GetComponent<VRInteractionInput>().isInteracting && photonView.IsMine)
            {
                photonView.TransferOwnership(masterClientID);

                VRInteractionEnd();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.transform.Equals(interactingOwner) && photonView.IsMine)
        {
            photonView.TransferOwnership(masterClientID);

            VRInteractionEnd();
        }
    }

    public virtual void VRInteractionBegin()
    {
        interactingOwner = null;
    }



    public virtual void VRInteractionEnd()
    { }

    [PunRPC]
    void BeginOwnerInteraction()
    {
        VRInteractionBegin();
    }


}
