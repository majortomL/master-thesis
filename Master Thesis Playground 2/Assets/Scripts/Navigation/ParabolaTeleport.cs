using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class ParabolaTeleport : MultiuserTeleport
{
    ParabolaCalculator parabola;
    public float rayVelocity = 10;
    public float rayTimeStep = 0.1f;
    public int rayMaxSegments = 20;
    public float dampenRayUpForce = 0.2f;

    bool neccessaryForValidLoc = true;

    public GameObject MeshParent;
    Transform productionCubes = null;

    public GameObject DataCollector;

    // Data Recording
    private DataCollector dataCollector;

    public override void Start()
    {
        // Get the data collector class
        dataCollector = DataCollector.GetComponent<DataCollector>();

        //base.acceptProtocol = GetComponent<ContinuousVote>();
        base.Start();
        parabola = GetComponent<ParabolaCalculator>();
        if (!parabola) parabola = gameObject.AddComponent<ParabolaCalculator>();

    }

    public override void Update()
    {
        //if (Time.timeSinceLevelLoad < 20.0f) return;
        base.Update();
    }

    protected override void selectTeleLocation() {
        Vector3 baseDir = (Vector3.forward + Vector3.up * 0.25f).normalized;
        Vector3 dir = planerTF.rotation * baseDir;

        float upDirDownScale = 1 - Mathf.Abs(Vector3.Dot(dir,Vector3.up));  // reduces ray velocity if controller points vertically
        upDirDownScale = (upDirDownScale + dampenRayUpForce) / (1 + dampenRayUpForce);

        Vector3 p0 = parabola.init(
            planerTF.position + dir * 0.05f,
            dir * rayVelocity * upDirDownScale,
            rayTimeStep
        );
        Vector3 p1 = parabola.next();

        List<Vector3> positions = new List<Vector3>();
        positions.Add(p0);

        RaycastHit hitInfo;
        while (positions.Count <= rayMaxSegments)
        {
            if (Physics.Raycast(p0, p1 - p0, out hitInfo, (p1 - p0).magnitude, ~IgnoreLayer))
            {
                p1 = hitInfo.point;
                neccessaryForValidLoc = layerInLayerMask(hitInfo.transform.gameObject.layer, FloorLayer);
                break;
            }
            p0 = p1;
            positions.Add(p0);
            p1 = parabola.next();
        }
        positions.Add(p1);

        Vector3 selectedPosition = p1;
        UserTargetInfo myTargetI = findUserTargetInfo(myActornumber());
        myTargetI.tf.position = p1;

        LineRenderer lr = myTargetI.tf.GetComponent<LineRenderer>();
        lr.positionCount = positions.Count;
        lr.SetPositions(positions.ToArray());

        teleportMovement = determineTeleportMovement(myTargetI.avatarHMD, myTargetI.tf);
    }

    protected override void updateRayPositions(int actorNR, UserTargetInfo userTargetI) {
        if(actorNR == myActornumber() && iAmMasterMindAndPlanning()) return;  // this already gets covered by selectTeleLocation()

        LineRenderer lr = userTargetI.tf.GetComponent<LineRenderer>();
        Transform origin = userTargetI.avatarHand();
        Transform target = userTargetI.tf;

        lr.positionCount = Mathf.Clamp((int)((origin.position - target.position).magnitude * 5), 2, 150);
        lr.SetPositions(parabola.BezierArc(origin, target, lr.positionCount - 1).ToArray());
    }

    bool layerInLayerMask(int layer, LayerMask layerMask) {
        return layerMask == (layerMask | (1 << layer));
    }
    protected override string getUserTargetIndicatorName(int actorNR) {
        return "UserTargetIndicatorPT" + actorNR;
    }

    protected override bool userTargetIsValid(int actorNR, UserTargetInfo userTarget) {
        if (iAmMasterMindAndPlanning()) {
            return neccessaryForValidLoc && base.userTargetIsValid(actorNR, userTarget);// && teleportStaysInSameCube(userTarget);
        }
        return base.userTargetIsValid(actorNR, userTarget);// && teleportStaysInSameCube(userTarget);
    }

    protected bool teleportStaysInSameCube(UserTargetInfo userTarget) {
        int hmdCube = getIDofCubeThatContainsPos(userTarget.avatarHMD.position);
        int targetCube = getIDofCubeThatContainsPos(userTarget.tf.position + Vector3.up * 0.1f);
        return hmdCube == targetCube;
    }

    private int getIDofCubeThatContainsPos(Vector3 pos) {
        if (!MeshParent) return -1;

        if(!productionCubes) {
            productionCubes = MeshParent.transform.Find("ProductionCubes");
        }


        if (productionCubes) {
            int childID = 0;
            foreach (Transform cube in productionCubes) {
                if (cube.GetComponent<BoxCollider>() != null && cube.GetComponent<BoxCollider>().bounds.Contains(pos)) {
                    return childID;
                }
                childID++;
            }
        }

        return -1;
    }

    #region data collection

    public override void teleportButtonReleased()
    {

        if (!pointingAtUI()) acceptProtocol.acceptButtonReleased();

        if (iAmMasterMindAndPlanning())
        {
            if (allUserTargetsAreValid() && !pointingAtUI())
            {
                photonView.RPC("requestTeleport", RpcTarget.MasterClient, teleportMovement);

                // Status 2 for data recording
                DataCollector.GetComponent<PhotonView>().RPC("updateTeleportStatus", RpcTarget.MasterClient, photonView.OwnerActorNr, 2, 0);
            }
            else
            {
                photonView.RPC("stopPlanning", RpcTarget.All);
            }

        }
    }

    [PunRPC]
    protected override void executeTeleport()
    {
        // Status 4 for data recording
        DataCollector.GetComponent<PhotonView>().RPC("updateTeleportStatus", RpcTarget.MasterClient, photonView.OwnerActorNr, 5, 0);
        base.executeTeleport();
    }

    #endregion
}
