using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Snap : MonoBehaviourPun
{
    public List<GameObject> Anchors = new List<GameObject>();
    public List<GameObject> PossibleTargets = new List<GameObject>();
    public List<GameObject> Snapped = new List<GameObject>();

    public float SnapDistance = 0.1f;
    public bool heldInHand = false;

    // Start is called before the first frame update
    void Start()
    {
        //List<string> snappableColors = new List<string>();

        foreach (Transform child in transform.Find("Anchors"))
        {
            if (child.name.Contains("Anchor"))
            {
                //snappableColors.Add(child.name.Replace("Anchor ", ""));
                Anchors.Add(child.gameObject);
            }
        }

        foreach (GameObject disposable in GameObject.FindGameObjectsWithTag("Grabbable"))
        {
            if (disposable.name.Contains("Disposable") && disposable != gameObject)
            {
                PossibleTargets.Add(disposable);
            }
        }
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            foreach (GameObject target in PossibleTargets)
            {
                if (!Snapped.Contains(target))
                {
                    checkSnap(target);
                }
            }
        }
    }

    void checkSnapOld(GameObject target)
    {
        foreach (GameObject snapper in Anchors)
        {
            foreach (GameObject targetSnapper in target.GetComponent<Snap>().Anchors)
            {
                if (targetSnapper.name.Contains(gameObject.name.Replace("Disposable ", "")) && Vector3.Distance(snapper.transform.position, targetSnapper.transform.position) < 0.1f && heldInHand)
                {
                    {

                        target.GetComponent<PhotonView>().RequestOwnership();

                        target.transform.position = snapper.transform.position - (targetSnapper.transform.position - target.transform.position);

                        FixedJoint joint = gameObject.AddComponent<FixedJoint>();
                        joint.breakTorque = Mathf.Infinity;
                        joint.breakForce = Mathf.Infinity;
                        joint.connectedBody = target.GetComponent<Rigidbody>();
                        joint.enableCollision = false;
                        Snapped.Add(target);
                    }
                }
            }
        }
    }

    void checkSnap(GameObject target)
    {
        foreach (GameObject anchor in Anchors)
        {
            foreach (GameObject targetAnchor in target.GetComponent<Snap>().Anchors)
            {
                if (Vector3.Distance(anchor.transform.position, targetAnchor.transform.position) < SnapDistance && heldInHand && !Snapped.Contains(target))
                {
                    target.GetComponent<PhotonView>().RequestOwnership();

                    target.transform.position = anchor.transform.position - targetAnchor.transform.position + target.transform.position;
                    transform.rotation = target.transform.rotation;
                    Debug.Break();
                    FixedJoint joint = gameObject.AddComponent<FixedJoint>();
                    joint.breakTorque = Mathf.Infinity;
                    joint.breakForce = Mathf.Infinity;
                    joint.connectedBody = target.GetComponent<Rigidbody>();
                    joint.enableCollision = false;
                    Snapped.Add(target);
                }
            }
        }
    }
}
