using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class InterCubeTeleport : MultiuserTeleport
{
    public string TeleportTargetTag = "Object";
    public GameObject ModelParent;
    public GameObject MeshParent;

    public Transform HMDPosition;
    public Transform RightControllerTransform;
    public Transform LeftControllerTransform;
    public MiniatureModelizerUserStudy MiniatureModelizer;
    public GameObject OriginGameObject;
    public GameObject ModelOriginGameObject;

    private string aimedTargetCube;
    private string localAimedTargetCube = "";
    private LineRenderer laserPointer;
    private Transform laserPointerEndpoint;
    private GameObject aimedCube;

    public GameObject DataCollector;

    private GameObject targetedObject = null;
    // Data Recording
    private DataCollector dataCollector;

    private float ratio = 0.02f;

    public override void Start()
    {
        // Get the data collector class
        dataCollector = DataCollector.GetComponent<DataCollector>();

        laserPointer = GetComponent<LineRenderer>();
        laserPointer.positionCount = 2;

        base.Start();
        status = TeleportStatus.INACTIVE;
    }

    public override void Update()
    {

        base.Update();
        if (status == TeleportStatus.PLANNING || status == TeleportStatus.INACTIVE)
        {
            if (checkRayHit())
            {
                //if (iAmMasterMindAndPlanning())
                //{
                //    photonView.RPC("syncTargetCube", RpcTarget.Others, localAimedTargetCube);
                //}
                //else if (status != TeleportStatus.PLANNING)
                //{
                //    photonView.RPC("initiatePlanning", RpcTarget.MasterClient, myActornumber());
                //}
            }
            //else if (iAmMasterMindAndPlanning())
            //{
            //    photonView.RPC("stopPlanning", RpcTarget.All);
            //}
        }
        else
        {
            if (laserPointerEndpoint)
            {
                PhotonNetwork.Destroy(laserPointerEndpoint.gameObject);
                localImplNowActive = false;
                thisImplNowActive = false;
            }
        }
    }

    private bool checkRayHit()
    {
        Vector3 baseDir = Vector3.forward;
        Vector3 dirRight = RightControllerTransform.rotation * baseDir;

        Vector3 p0right = RightControllerTransform.position;
        Vector3 p1right = RightControllerTransform.position + 10 * dirRight;
        RaycastHit hitInfo;


        if (Physics.Raycast(p0right, dirRight, out hitInfo, (p1right - p0right).magnitude, ~IgnoreLayer)) // Perform RayCast
        {
            if (hitInfo.transform.tag == TeleportTargetTag || hitInfo.transform.gameObject.name == "Table") // Hitting table or cube
            {
                // === Data Collection === 
                if (hitInfo.transform.gameObject != targetedObject && PhotonNetwork.IsConnected)
                {
                    targetedObject = hitInfo.transform.gameObject;
                    DataCollector.GetComponent<PhotonView>().RPC("updateModelPlanning", RpcTarget.MasterClient, true);
                }
                // === Data Collection End ===

                if (laserPointerEndpoint == null && myActornumber() != -1) // Init stuff
                {
                    object[] initData = new object[2] { PhotonNetwork.LocalPlayer.NickName, new Vector3(getUserColor(myActornumber()).r, getUserColor(myActornumber()).g, getUserColor(myActornumber()).b) };
                    laserPointerEndpoint = PhotonNetwork.Instantiate("Lasertarget", hitInfo.point, Quaternion.identity, 0, initData).transform;
                    localImplNowActive = true;
                    thisImplNowActive = true;
                }

                if (laserPointerEndpoint) // set endpoint a bit higher
                {
                    laserPointerEndpoint.position = hitInfo.point + new Vector3(0, 0.01f, 0);
                }

                if (hitInfo.transform.tag == TeleportTargetTag) // hitting cube
                {
                    string subProcess = hitInfo.transform.gameObject.GetComponent<ProductionCube>().SubProcess;
                    if (!MiniatureModelizer.findTooltipWithSubProcess(subProcess).activeSelf)
                    {
                        photonView.RPC("activateTooltip", RpcTarget.All, subProcess);
                    }

                    if (aimedCube != null)
                    {
                        if (MiniatureModelizer.findTooltipWithSubProcess(aimedCube.GetComponent<ProductionCube>().SubProcess).activeSelf && aimedCube != hitInfo.transform.gameObject)
                        {
                            photonView.RPC("deactivateTooltip", RpcTarget.All, aimedCube.GetComponent<ProductionCube>().SubProcess);
                        }
                    }

                    aimedCube = hitInfo.transform.gameObject;

                    localAimedTargetCube = hitInfo.transform.gameObject.GetComponent<ProductionCube>().SubProcess;
                    if (iAmMasterMindAndPlanning())
                    {
                        photonView.RPC("syncTargetCube", RpcTarget.All, localAimedTargetCube);

                        Vector3 tfPosition = hitInfo.transform.gameObject.GetComponent<BoxCollider>().bounds.center;
                        tfPosition.y = hitInfo.transform.gameObject.GetComponent<BoxCollider>().bounds.center.y - hitInfo.transform.gameObject.GetComponent<BoxCollider>().bounds.extents.y;

                        findUserTargetInfo(myActornumber()).tf.position = tfPosition;
                        //float ratio = 0.02f;
                        findUserTargetInfo(myActornumber()).tf.localScale = 4f * new Vector3(ratio, ratio, ratio);
                        findUserTargetInfo(myActornumber()).tf.GetComponent<LineRenderer>().widthMultiplier = 0.15f;
                    }

                    GameObject targetObject = findProductionCubeWithSubProcess(hitInfo.transform.gameObject.GetComponent<ProductionCube>().SubProcess, MeshParent);

                    Vector3 targetPosition;

                    if (targetObject != null) // we are aiming at a production cube
                    {
                        targetPosition = targetObject.GetComponent<BoxCollider>().bounds.center;
                    }
                    else // we are aiming at the origin cube
                    {
                        targetPosition = OriginGameObject.GetComponent<BoxCollider>().bounds.center;
                    }

                    if (iAmMasterMindAndPlanning())
                    {
                        teleportMovement = determineTeleportMovement(HMDPosition, targetPosition);
                    }

                    return true;
                }
                else if (aimedCube != null) // hitting table
                {
                    localAimedTargetCube = "";
                    if (MiniatureModelizer.findTooltipWithSubProcess(aimedCube.GetComponent<ProductionCube>().SubProcess).activeSelf)
                    {
                        photonView.RPC("deactivateTooltip", RpcTarget.All, aimedCube.GetComponent<ProductionCube>().SubProcess);
                        aimedCube = null;
                    }
                }
                return false;
            }
            else if (aimedCube != null) // hitting neither cube nor table but hitting something
            {
                if (MiniatureModelizer.findTooltipWithSubProcess(aimedCube.GetComponent<ProductionCube>().SubProcess).activeSelf)
                {
                    photonView.RPC("deactivateTooltip", RpcTarget.All, aimedCube.GetComponent<ProductionCube>().SubProcess);
                    aimedCube = null;
                }
            }
        }
        else if (aimedCube != null) // Hitting nothing at all
        {
            if (MiniatureModelizer.findTooltipWithSubProcess(aimedCube.GetComponent<ProductionCube>().SubProcess).activeSelf)
            {
                photonView.RPC("deactivateTooltip", RpcTarget.All, aimedCube.GetComponent<ProductionCube>().SubProcess);
                aimedCube = null;
            }
            // === Data Collection === 
            DataCollector.GetComponent<PhotonView>().RPC("updateModelPlanning", RpcTarget.MasterClient, false);
            // === Data Collection End === 
        }
        if (laserPointerEndpoint)
        {
            PhotonNetwork.Destroy(laserPointerEndpoint.gameObject);
            localImplNowActive = false;
            thisImplNowActive = false;
        }

        return false;
    }

    public override void cancelButtonPressed()
    {
        base.cancelButtonPressed();
    }

    [PunRPC]
    protected override void executeTeleport()
    {
        // Status 4 for data recording
        DataCollector.GetComponent<PhotonView>().RPC("updateTeleportStatus", RpcTarget.MasterClient, photonView.OwnerActorNr, 0, 5);
        base.executeTeleport();
    }

    protected Vector3 determineTeleportMovement(Transform HMD, Vector3 target)
    {
        Vector3 dir = target - HMD.position;
        float floorOffset = 0f;
        RaycastHit findFloorDown;
        if (Physics.Raycast(HMD.position, Vector3.down, out findFloorDown, 50f, FloorLayer)) floorOffset = findFloorDown.distance;

        return dir + Vector3.up * floorOffset;
    }

    [PunRPC]
    public void syncTargetCube(string targetCube)
    {
        aimedTargetCube = targetCube;
    }

    [PunRPC]
    public void activateTooltip(string subProcess)
    {
        MiniatureModelizer.activateTooltip(subProcess);
    }

    [PunRPC]
    public void deactivateTooltip(string subProcess)
    {
        MiniatureModelizer.deactivateTooltip(subProcess);
    }

    protected override void updateMyTargetLocation(UserTargetInfo userTargetI)
    {
        if (!iAmMasterMindAndPlanning() && status != TeleportStatus.INACTIVE)
        {
            Transform productionCubesParent = MiniatureModelizer.transform.FindChildRecursive("Model");
            if (productionCubesParent)
            {
                GameObject productionCubesParentGO = productionCubesParent.gameObject;
                Vector3 masterMindUserHMDPosition = findUserTargetInfo(masterMindUser).avatarHMD.position;
                Vector3 differenceMasterMindUserMe = HMDPosition.position - masterMindUserHMDPosition;
                GameObject productionCubeGameObject;
                if (aimedTargetCube != "Origin Location")
                {
                    productionCubeGameObject = findProductionCubeWithSubProcess(aimedTargetCube, productionCubesParentGO);
                }
                else
                {
                    productionCubeGameObject = ModelOriginGameObject;
                }

                if (productionCubeGameObject != null)
                {
                    //float ratio = 0.02f;
                    Vector3 productionCubePosition = productionCubeGameObject.GetComponent<BoxCollider>().bounds.center;
                    productionCubePosition.y = productionCubeGameObject.GetComponent<BoxCollider>().bounds.center.y - productionCubeGameObject.GetComponent<BoxCollider>().bounds.extents.y;
                    userTargetI.tf.position = productionCubePosition + ratio * differenceMasterMindUserMe;
                    userTargetI.tf.localScale = 4f * new Vector3(ratio, ratio, ratio);
                }
            }
        }
    }

    protected override void updateRayPositions(int actorNR, UserTargetInfo userTargetI)
    {
        base.updateRayPositions(actorNR, userTargetI);
        LineRenderer lr = userTargetI.tf.GetComponent<LineRenderer>();

        if (masterMindUser == actorNR)
        {
            lr.widthMultiplier = 0.15f;
        }
        else
        {
            lr.widthMultiplier = 0.05f;
        }
    }

    private GameObject findProductionCubeWithSubProcess(string subProcess, GameObject searchParent)
    {
        //Transform productionCubes = searchParent.transform.Find("ProductionCubes");
        Transform productionCubes = searchParent.transform.FindChildRecursive("ProductionCubes");

        if (productionCubes != null)
        {
            foreach (Transform productionCube in productionCubes)
            {
                if (productionCube.GetComponent<ProductionCube>().SubProcess == subProcess)
                {
                    return productionCube.gameObject;
                }
            }
        }
        return null;
    }

    public override void teleportButtonPressed(Transform t)
    {
        base.teleportButtonPressed(t);
    }

    public override void teleportButtonReleased()
    {
        if (!pointingAtUI()) acceptProtocol.acceptButtonReleased();

        if (iAmMasterMindAndPlanning())
        {
            if (allUserTargetsAreValid() && !pointingAtUI())
            {
                if (localAimedTargetCube != "")
                {
                    photonView.RPC("requestTeleport", RpcTarget.MasterClient, teleportMovement);

                    // Status 2 for data recording
                    DataCollector.GetComponent<PhotonView>().RPC("updateTeleportStatus", RpcTarget.MasterClient, photonView.OwnerActorNr, 0, 2);
                }
            }
            else
            {
                photonView.RPC("stopPlanning", RpcTarget.All);
            }
        }
    }
}