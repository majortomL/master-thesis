using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class BinController : MonoBehaviourPun
{
    public ParticleSystem ParticleSystem;
    public List<GameObject> Disposables;
    public int NumberOfBlueprints;

    private Bounds binBounds;
    private int disposablesCollected = 0;
    private bool updated = false;
    public BlueprintTracker blueprintTracker;

    // Start is called before the first frame update
    void Start()
    {
        Disposables = new List<GameObject>();
        binBounds = GetComponent<MeshRenderer>().bounds;
        blueprintTracker = GameObject.Find("MiniatureModel").transform.Find("Canvas").GetComponent<BlueprintTracker>();
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.AmOwner && !updated)
        {
            checkContents();
        }
        updated = false;
    }

    public void UpdateDisposables()
    {
        Disposables.Clear();
        GameObject[] Disp = GameObject.FindGameObjectsWithTag("Grabbable");
        foreach (GameObject disp in Disp)
        {
            if (disp.name.Contains("Blueprint"))
            {
                Disposables.Add(disp);
            }
        }
    }

    void checkContents()
    {
        if (Disposables.Count > 0)
        {
            List<GameObject> disposables = new List<GameObject>(Disposables);
            foreach (GameObject disposable in disposables)
            {
                Vector3 vectorSum = Vector3.zero;
                int numberChildren = 0;

                foreach (Transform child in disposable.transform)
                {
                    if (numberChildren == 0)
                    {
                        vectorSum = child.position;
                    }
                    else
                    {
                        vectorSum += child.position;
                    }
                    numberChildren++;
                }

                Vector3 centerPosition = vectorSum / numberChildren;


                //if (binBounds.Contains(centerPosition) && disposable.activeInHierarchy)
                if(Vector3.Distance(transform.position, centerPosition) < 0.8f)
                {
                    photonView.RPC("fireParticles", RpcTarget.All);
                    photonView.RPC("deactivateDisposable", RpcTarget.All, disposable.name);
                    photonView.RPC("updateDisposablesCollected", RpcTarget.All, disposablesCollected + 1);
                }
            }
        }
    }

    [PunRPC]
    public void fireParticles()
    {
        ParticleSystem.Play();
    }

    [PunRPC]
    public void deactivateDisposable(string disposableId)
    {
        foreach (GameObject disposable in Disposables)
        {
            if (disposable.activeInHierarchy && disposable.name == disposableId)
            {
                disposable.SetActive(false);
            }
        }
        UpdateDisposables();
        updated = true;
    }

    [PunRPC]
    public void updateDisposablesCollected(int count)
    {
        disposablesCollected = count;
        blueprintTracker.CompleteBlueprints = disposablesCollected;
    }
}
