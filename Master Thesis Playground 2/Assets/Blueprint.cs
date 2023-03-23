using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Blueprint : MonoBehaviour
{
    public GameObject[] Parts;
    // Dict: Part, isPlaced
    public Dictionary<GameObject, bool> BlueprintParts = new Dictionary<GameObject, bool>();
    public float SnapDistance = 0.1f;

    public Material MaterialGreen;
    public Material MaterialRed;
    public Material MaterialYellow;
    public Material MaterialBlue;

    public GameObject RightOVRController;
    public BinController BinController;

    public ParticleSystem particles;

    private int numberParts = 0;
    private int numberPartsSet = 0;
    public bool isComplete = false;

    // Start is called before the first frame update
    void Start()
    {
        BinController = GameObject.Find("Bin").GetComponent<BinController>();
        particles = transform.Find("Particle System").GetComponent<ParticleSystem>();
        foreach (Transform child in transform)
        {
            if (child.name != "Particle System")
            {
                BlueprintParts.Add(child.gameObject, false);
                numberParts += 1;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameObject addedPart = null;

        foreach (KeyValuePair<GameObject, bool> blueprintPart in BlueprintParts)
        {
            foreach (GameObject part in Parts)
            {
                // Distance is small enough, colors match, target part is not set yet, blueprint is not finished yet, the cube's photonview i am adding is mine.
                if (
                    Vector3.Distance(blueprintPart.Key.transform.position, part.transform.position) < SnapDistance &&
                    blueprintPart.Key.GetComponent<Disposable>().color == part.GetComponent<Disposable>().color &&
                    blueprintPart.Value == false
                    )
                {
                    // Take over ownership of the blueprint
                    GetComponent<PhotonView>().RequestOwnership();

                    // Activate the part of the blueprint
                    addedPart = activatePart(blueprintPart);

                    // Deactivate the part held in hand
                    deactivatePart(part);
                }
            }
        }

        // Mark the blueprint part as placed
        if (addedPart != null)
        {
            BlueprintParts[addedPart] = true;
        }
    }

    GameObject activatePart(KeyValuePair<GameObject, bool> blueprintPart)
    {
        // Make the blueprint part opaque
        switch (blueprintPart.Key.GetComponent<Disposable>().color)
        {
            case "green":
                blueprintPart.Key.transform.Find("Frame").GetComponent<Renderer>().material = MaterialGreen;
                break;
            case "red":
                blueprintPart.Key.transform.Find("Frame").GetComponent<Renderer>().material = MaterialRed;
                break;
            case "yellow":
                blueprintPart.Key.transform.Find("Frame").GetComponent<Renderer>().material = MaterialYellow;
                break;
            case "blue":
                blueprintPart.Key.transform.Find("Frame").GetComponent<Renderer>().material = MaterialBlue;
                break;
        }
        GameObject addedPart = blueprintPart.Key;
        numberPartsSet += 1;
        isComplete = numberPartsSet == numberParts;

        // Blueprint got completed this iteration
        if (isComplete)
        {
            makeBlueprintGrabbable();
            fireParticles();
        }

        GetComponent<PhotonView>().RPC("syncActivateBlueprintPart", RpcTarget.Others, blueprintPart.Key.name);
        return addedPart;
    }

    void deactivatePart(GameObject part)
    {
        // Delete the original object
        part.SetActive(false);

        GetComponent<PhotonView>().RPC("syncDeactivateCube", RpcTarget.Others, part.name);
    }

    [PunRPC]
    void syncDeactivateCube(string cubeKey)
    {
        // Deactivate cubeKey Cube in the scene
        GameObject.Find(cubeKey).SetActive(false);
    }

    [PunRPC]
    void syncActivateBlueprintPart(string partKey)
    {
        // Get the Cube of the blueprint we want to activate
        GameObject blueprintPart = GameObject.Find(partKey);

        // render the cube we need opaque
        switch (blueprintPart.GetComponent<Disposable>().color)
        {
            case "green":
                blueprintPart.transform.Find("Frame").GetComponent<Renderer>().material = MaterialGreen;
                break;
            case "red":
                blueprintPart.transform.Find("Frame").GetComponent<Renderer>().material = MaterialRed;
                break;
            case "yellow":
                blueprintPart.transform.Find("Frame").GetComponent<Renderer>().material = MaterialYellow;
                break;
            case "blue":
                blueprintPart.transform.Find("Frame").GetComponent<Renderer>().material = MaterialBlue;
                break;
        }

        GameObject addedPart = blueprintPart;
        numberPartsSet += 1;
        isComplete = numberPartsSet == numberParts;

        if (isComplete)
        {
            makeBlueprintGrabbable();
            fireParticles();
        }

        BlueprintParts[addedPart] = true;
    }


    void makeBlueprintGrabbable() // Blueprint is completed here
    {
        foreach(GameObject blueprintPart in BlueprintParts.Keys)
        {
            BoxCollider colliderAdded = gameObject.AddComponent<BoxCollider>();
            colliderAdded.size = blueprintPart.GetComponent<BoxCollider>().size;
            colliderAdded.center = transform.InverseTransformPoint(blueprintPart.transform.TransformPoint(blueprintPart.GetComponent<BoxCollider>().center));
        }

        Rigidbody rigidBodyAdded = gameObject.AddComponent<Rigidbody>();
        rigidBodyAdded.useGravity = false;
        rigidBodyAdded.drag = 1;
        rigidBodyAdded.angularDrag = 500;

        gameObject.tag = "Grabbable";
        BinController.UpdateDisposables();
        RightOVRController.GetComponent<OVRControllerInterface>().grabbables.Add(gameObject);
    }

    void fireParticles()
    {
        particles.Play();
    }

    public void takeoverOwnership()
    {
        Debug.Log("Taking Blueprint Ownership of " + this.name);
        foreach(KeyValuePair<GameObject, bool> blueprintPart in BlueprintParts)
        {
            blueprintPart.Key.GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
}
