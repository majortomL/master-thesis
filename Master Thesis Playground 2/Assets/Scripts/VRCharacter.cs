using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class VRCharacter : MonoBehaviourPun, IPunObservable
{

    private VRPlayerManager playerManager;

    public Transform head;
    public Transform left;
    public Transform right;
    public Transform body;
    public Transform pointer;
    public Transform headSphere;

    Transform headCtr;
    Transform leftCtr;
    Transform rightCtr;
    Transform bodyCtr;

    private bool leftActive = false;
    private bool rightActive = false;

    private bool localCharacter = false;


    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Would need to be observed by a PhotonView on the prefab

        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            //   stream.SendNext(Object obj); // can observe any variable
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            stream.SendNext(head.position);
            stream.SendNext(head.rotation);

            stream.SendNext(left.position);
            stream.SendNext(left.rotation);

            stream.SendNext(right.position);
            stream.SendNext(right.rotation);

            stream.SendNext(body.position);
            stream.SendNext(body.rotation);

            stream.SendNext(pointer.position);
            stream.SendNext(pointer.rotation);
            stream.SendNext(pointer.GetChild(0).position);
        }
        else
        {
            // Network player, receive data
            //   this.obj = (Object)stream.ReceiveNext(); // receive the same variable from the stream if not local player
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();

            head.position = (Vector3)stream.ReceiveNext();
            head.rotation = (Quaternion)stream.ReceiveNext();

            left.position = (Vector3)stream.ReceiveNext();
            left.rotation = (Quaternion)stream.ReceiveNext();

            right.position = (Vector3)stream.ReceiveNext();
            right.rotation = (Quaternion)stream.ReceiveNext();

            body.position = (Vector3)stream.ReceiveNext();
            body.rotation = (Quaternion)stream.ReceiveNext();

            pointer.position = (Vector3)stream.ReceiveNext();
            pointer.rotation = (Quaternion)stream.ReceiveNext();
            pointer.GetChild(0).position = (Vector3)stream.ReceiveNext();
        }
    }


    void Start()
    {
        playerManager = GetComponentInParent<VRPlayerManager>();

        if (playerManager.photonView.IsMine)
        {
            //body.FindChildRecursive("default").GetComponent<MeshRenderer>().enabled = false;

            localCharacter = true;

            headCtr = GameObject.FindGameObjectWithTag("MainCamera").transform;

            if (left)
                leftCtr = GameObject.FindGameObjectWithTag("Left").transform;
            if (leftCtr)
            {
                SetLeftActive(true);
                left.GetComponent<MeshRenderer>().enabled = false;   // hide controller since OVRCameraRig already contains a visual representation for the local player
            }

            if (right)
                rightCtr = GameObject.FindGameObjectWithTag("Right").transform;
            if (rightCtr)
            {
                SetRightActive(true);
                right.GetComponent<MeshRenderer>().enabled = false;  // hide controller since OVRCameraRig already contains a visual representation for the local player
            }

            if (pointer)
            {
                //OVRCursor cursor = pointer.GetComponent<OVRCursor>();
                //GameObject.Find("EventSystem").GetComponent<OVRInputModule>().m_Cursor = cursor;
                //pointer.GetComponent<OVRGazePointer>().rayTransform = rightCtr;
            }

            bodyCtr = GameObject.FindGameObjectWithTag("MainCamera").transform;
        }
        else
        {
        }


    }

    // Update is called once per frame
    void Update()
    {

        if (localCharacter)
        {
            UpdateCharacterHead();
            UpdateCharacterBody();
            if (leftActive)
                UpdateCharacterLeft();
            if (rightActive)
                UpdateCharacterRight();
        }

    }

    void UpdateCharacterHead()
    {
        head.position = headCtr.position;
        head.rotation = headCtr.rotation;
    }
    void UpdateCharacterBody()
    {
        Vector3 handsLine = leftCtr.position - rightCtr.position;
        body.eulerAngles = new Vector3(0, -Vector3.SignedAngle(handsLine, Vector3.forward, Vector3.up), 0);
        body.position = headSphere.position - new Vector3(0, 1.4f, 0);

    }

    void UpdateCharacterLeft()
    {
        left.position = leftCtr.position;
        left.rotation = leftCtr.rotation;
    }

    void UpdateCharacterRight()
    {
        right.position = rightCtr.position;
        right.rotation = rightCtr.rotation;
    }

    public void SetLeftActive(bool active)
    {
        left.gameObject.SetActive(active);
        leftActive = active;
    }

    public void SetRightActive(bool active)
    {
        right.gameObject.SetActive(active);
        rightActive = active;
    }
}
