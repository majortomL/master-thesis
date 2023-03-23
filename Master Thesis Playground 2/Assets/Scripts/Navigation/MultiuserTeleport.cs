using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

using Photon.Pun;
using Photon.Realtime;

using Player = Photon.Realtime.Player;

public abstract class MultiuserTeleport : MonoBehaviourPunCallbacks
{
    public enum TeleportStatus
    {
        INACTIVE = 0,
        PLANNING = 1,
        REQUESTED = 2,
        IMMINENT = 3
    }

    public enum UserRelevance
    {
        PLANNING_USER = 0,
        OTHER_USER = 1,
    }

    // information about the target of a user
    // transforms remain constant during execution
    // properties change depending on current statemachine state
    protected struct UserTargetInfo
    {
        public Transform avatarHMD;
        public Transform avatarLHand;
        public Transform avatarRHand;
        public Transform tf;
        public Transform shirt;


        public UserTargetProperties prop;

        public Transform avatarHand() { return prop.useLeftHand ? avatarLHand : avatarRHand; }

        public UserTargetInfo(Transform avHMD, Transform avLHand, Transform avRHand, Transform trf, Transform shirt, UserTargetProperties p)
        {
            avatarHMD = avHMD;
            avatarLHand = avLHand;
            avatarRHand = avRHand;
            this.shirt = shirt;
            this.tf = trf;
            prop = p;
        }
    }

    protected struct UserTargetProperties
    {
        public UserRelevance relevance;
        public Material rayMaterial;
        public Material markerMaterial;
        public bool useLeftHand;

        public bool wasValid;   // unsynchronized, updated by local user

        public UserTargetProperties(UserRelevance rel, Material rMat, Material mMat, bool leftHand = false)
        {
            relevance = rel;
            rayMaterial = rMat;
            markerMaterial = mMat;
            useLeftHand = leftHand;
            wasValid = false;
        }
    }

    public Transform VRRig;
    public VoteProtocol acceptProtocol;
    public bool awaitFinalVerdict = true;
    public double mutableVerdictCooldown = 3.0f;
    public GameObject[] nonUserTeleportObjects;

    public Material validMarkerMaterial;
    public Material invalidMarkerMaterial;
    public Material validRayMaterial;
    public Material invalidRayMaterial;
    public Material controllerTemplateMat;
    public Material shirtTemplateMat;
    public Color[] userColorList = new Color[] {
        new Color(0.572549f, 0, 0, 1),
        new Color(0, 0.427451f, 0.8588235f, 1),
        new Color(0.1333333f, 0.8117647f, 0.1333333f, 1),
        new Color(1, 0.8745098f, 0.3019608f, 1)
    };
    private Dictionary<int, int> userColorAttribution = new Dictionary<int, int>();
    //public LayerMask IgnoreLayer;
    protected LayerMask IgnoreLayer = 1 << 8 | 1 << 10;
    public LayerMask FloorLayer;


    protected static bool anyImplNowActive;
    protected static bool localImplNowActive;
    protected bool thisImplNowActive;
    public TeleportStatus status;

    public int masterMindUser = -1;
    protected Transform planerTF;   // contains Transform of which ever local GameObject initiated planning IF this user started planning; null if not currently planning or other user is planning
    protected Vector3 teleportMovement;
    double teleportTimestamp;   // used to schedule a teleport; does not get used if awaitFinalVerdict is true

    private Dictionary<int, UserTargetInfo> userTargetInfos = new Dictionary<int, UserTargetInfo>();
    private bool userTargetAlreadyInstantiated = false;


    public bool showPlannedThisUserRay;
    public bool showPlannedThisUserMarker;
    public bool showPlannedRequestingUserRay;
    public bool showPlannedRequestingUserMarker;
    public bool showPlannedOtherUserRay;
    public bool showPlannedOtherUserMarker;

    public bool showRequestedThisUserRay;
    public bool showRequestedThisUserMarker;
    public bool showRequestedRequestingUserRay;
    public bool showRequestedRequestingUserMarker;
    public bool showRequestedOtherUserRay;
    public bool showRequestedOtherUserMarker;

    // Start is called before the first frame update
    virtual public void Start()
    {
        acceptProtocol.pendingCallback += pendingCallback;
        acceptProtocol.mutableCallback += mutableCallback;
        acceptProtocol.verdictCallback += verdictCallback;

        teleportTimestamp = double.MaxValue;

        NavigationEvents.teleportButtonPressed.AddListener(teleportButtonPressed);
    }

    #region UPDATE FUNCTIONS
    // Update is called once per frame
    virtual public void Update()
    {
        //if (Time.timeSinceLevelLoad < 2.0f) return;

        switch (status)
        {
            case TeleportStatus.PLANNING:
                if (myActornumber() == masterMindUser)
                {
                    selectTeleLocation();
                }
                break;
            case TeleportStatus.REQUESTED:
                break;
            case TeleportStatus.IMMINENT:
                // maybe increase vibration strength upto
                if (PhotonNetwork.IsMasterClient)
                {
                    if (PhotonNetwork.Time >= teleportTimestamp)
                    {
                        this.photonView.RPC("executeTeleport", RpcTarget.All);
                    }
                }
                break;
        }

        updateTargetDisplay();
    }

    protected virtual void selectTeleLocation()
    {

    }

    public void updateTargetDisplay()
    {
        ensureUserTargetInfosIsUpToDate();
        Dictionary<int, UserTargetInfo> updatedDictionary = new Dictionary<int, UserTargetInfo>();
        foreach (var kvp in userTargetInfos)
        {
            int actorNR = kvp.Key;
            UserTargetInfo userTargetI = kvp.Value;

            if (actorNR == myActornumber()) updateMyTargetLocation(userTargetI);

            bool isValid = userTargetIsValid(actorNR, userTargetI);
            bool changeMat = userTargetI.prop.wasValid != isValid;

            bool rayVisible = shouldDrawRay(actorNR, userTargetI);
            setRayVisibility(userTargetI, rayVisible);
            if (changeMat) updateRayMat(actorNR, userTargetI, isValid);
            if (rayVisible) updateRayPositions(actorNR, userTargetI);

            bool markerVisible = shouldDrawMarker(actorNR, userTargetI);
            setMarkerVisibility(userTargetI, markerVisible);
            if (changeMat) updateMarkerMat(actorNR, userTargetI, isValid);
            if (markerVisible) updateMarkerNetwork(actorNR, userTargetI);

            userTargetI.prop.wasValid = isValid;
            updatedDictionary[actorNR] = userTargetI;
        }
        userTargetInfos = updatedDictionary;
    }

    protected virtual void updateMyTargetLocation(UserTargetInfo userTargetI)
    {
        int actorNR = myActornumber();
        if (masterMindUser == actorNR) return;   // this is handled in selectTeleLocation()

        if (status == TeleportStatus.PLANNING)
        {
            // approximate teleport movement based on mastermind user
            UserTargetInfo MMtarget = findUserTargetInfo(masterMindUser);
            teleportMovement = determineTeleportMovement(MMtarget.avatarHMD, MMtarget.tf);

            // get teleported position
            UserTargetInfo myTarget = findUserTargetInfo(actorNR);
            myTarget.tf.position = getFutureUserFeetPos(actorNR, myTarget);

        }
        else if (status == TeleportStatus.REQUESTED)
        {
            // temeportMovement is already set
            // get teleported position
            UserTargetInfo myTarget = findUserTargetInfo(actorNR);
            myTarget.tf.position = getFutureUserFeetPos(actorNR, myTarget);
        }
    }

    protected void setRayVisibility(UserTargetInfo userTargetI, bool visible)
    {
        userTargetI.tf.GetComponent<LineRenderer>().enabled = visible;
    }

    protected void setMarkerVisibility(UserTargetInfo userTargetI, bool visible)
    {
        userTargetI.tf.GetChild(0).gameObject.SetActive(visible);
        userTargetI.tf.GetChild(1).GetComponent<LineRenderer>().enabled = visible;
    }

    protected virtual void updateRayMat(int actorNR, UserTargetInfo userTargetI, bool isValid)
    {
        userTargetI.prop.rayMaterial = makeMaterialVariant(
            isValid,
            validRayMaterial,
            invalidRayMaterial,
            getUserColor(actorNR)
        );

        userTargetI.tf.GetComponent<LineRenderer>().material = userTargetI.prop.rayMaterial;
    }

    protected virtual void updateRayPositions(int actorNR, UserTargetInfo userTargetI)
    {
        Transform start = userTargetI.avatarHand();
        Transform end = userTargetI.tf;

        LineRenderer lr = end.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start.position);
        lr.SetPosition(1, end.position);
    }

    protected virtual void updateMarkerNetwork(int actorNR, UserTargetInfo userTargetI)
    {
        UserTargetInfo masterMindMarker = findUserTargetInfo(masterMindUser);
        LineRenderer lr = userTargetI.tf.GetChild(1).GetComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, userTargetI.tf.position + Vector3.up * 0.001f);
        lr.SetPosition(1, masterMindMarker.tf.position + Vector3.up * 0.001f);
    }

    protected virtual void updateMarkerMat(int actorNR, UserTargetInfo userTargetI, bool isValid)
    {
        userTargetI.prop.markerMaterial = makeMaterialVariant(
            isValid,
            validMarkerMaterial,
            invalidMarkerMaterial,
            getUserColor(actorNR)
        );
        userTargetI.tf.GetComponentInChildren<MeshRenderer>(true).material = userTargetI.prop.markerMaterial;

        Material networkMaterial = makeMaterialVariant(
            false,  // always use invalid material
            invalidRayMaterial,
            invalidRayMaterial,
            getUserColor(actorNR)
        );

        userTargetI.tf.GetChild(1).GetComponent<LineRenderer>().material = networkMaterial;
    }

    bool shouldDrawRay(int actorNR, UserTargetInfo userTargetI)
    {
        if (status == TeleportStatus.INACTIVE) return false;
        bool planning = status == TeleportStatus.PLANNING;
        bool showBecauseMyUser = (actorNR == myActornumber()) && (planning ? showPlannedThisUserRay : showRequestedThisUserRay);

        switch (userTargetI.prop.relevance)
        {
            case UserRelevance.PLANNING_USER:
                return showBecauseMyUser || (planning ? showPlannedRequestingUserRay : showRequestedRequestingUserRay);
            case UserRelevance.OTHER_USER:
                return showBecauseMyUser || (planning ? showPlannedOtherUserRay : showRequestedOtherUserRay);
            default:
                return true;
        }
    }

    bool shouldDrawMarker(int actorNR, UserTargetInfo userTargetI)
    {
        if (status == TeleportStatus.INACTIVE) return false;
        bool planning = status == TeleportStatus.PLANNING;
        bool showBecauseMyUser = (actorNR == myActornumber()) && (planning ? showPlannedThisUserMarker : showRequestedThisUserMarker);

        switch (userTargetI.prop.relevance)
        {
            case UserRelevance.PLANNING_USER:
                return showBecauseMyUser || (planning ? showPlannedRequestingUserMarker : showRequestedRequestingUserMarker);
            case UserRelevance.OTHER_USER:
                return showBecauseMyUser || (planning ? showPlannedOtherUserMarker : showRequestedOtherUserMarker);
            default:
                return true;
        }
    }

    // returns the world space position of where the users feet should land after teleporting
    Vector3 getFutureUserFeetPos(int actorNR, UserTargetInfo userTargetI)
    {
        Vector3 pos = userTargetI.avatarHMD.position;
        pos += teleportMovement;

        RaycastHit findFloorUp;
        const float searchDist = 50f;
        float upDist = searchDist;
        if (Physics.Raycast(pos, Vector3.up, out findFloorUp, searchDist, FloorLayer)) upDist = findFloorUp.distance;

        RaycastHit findFloorDown;
        float downDist = searchDist;
        if (Physics.Raycast(pos, Vector3.down, out findFloorDown, searchDist, FloorLayer)) downDist = findFloorDown.distance;

        if (upDist < downDist) pos = findFloorUp.point;
        else if (downDist < searchDist) pos = findFloorDown.point;

        return pos;
    }

    Vector3 getVRRigMovementVector()
    {
        int aNR = myActornumber();
        UserTargetInfo myTargetI = findUserTargetInfo(aNR);

        Vector3 currentHeadWPos = myTargetI.avatarHMD.position;
        Vector3 currentFeetWPos = currentHeadWPos;

        RaycastHit floorCast;
        const float searchDist = 5f;
        if (Physics.Raycast(currentHeadWPos, Vector3.down, out floorCast, searchDist, FloorLayer)) currentFeetWPos += Vector3.down * floorCast.distance;
        else currentFeetWPos += Vector3.down;

        Vector3 futureFeetWPos = getFutureUserFeetPos(aNR, myTargetI);

        return futureFeetWPos - currentFeetWPos;
    }

    protected virtual Vector3 determineTeleportMovement(Transform HMD, Transform target)
    {
        Vector3 dir = target.position - HMD.position;

        float floorOffset = 0f;
        RaycastHit findFloorDown;
        if (Physics.Raycast(HMD.position, Vector3.down, out findFloorDown, 50f, FloorLayer)) floorOffset = findFloorDown.distance;

        return dir + Vector3.up * floorOffset;
    }

    #endregion

    #region USER INTERACTION FUNCTIONS
    // call in the first frame that the teleport button is pressed
    public virtual void teleportButtonPressed(Transform t)
    {
        if (!pointingAtUI())
        {
            planerTF = t;
            if (status != TeleportStatus.REQUESTED)
            {
                acceptProtocol.accceptButtonPressed();
            }
            if (status == TeleportStatus.REQUESTED)
            {
                // only allow accepting the planned teleport if the current target is valid
                if (userTargetIsValid(myActornumber(), findUserTargetInfo(myActornumber())))
                {
                    acceptProtocol.accceptButtonPressed();
                }

            }

            switch (status)
            {
                case TeleportStatus.INACTIVE:
                    if (!MultiuserTeleport.anyImplNowActive && (!MultiuserTeleport.localImplNowActive || thisImplNowActive))
                        photonView.RPC("initiatePlanning", RpcTarget.MasterClient, myActornumber());
                    break;
            }
        }
    }

    // call in the frame that the teleport button is released
    public virtual void teleportButtonReleased()
    {

        if (!pointingAtUI()) acceptProtocol.acceptButtonReleased();

        if (iAmMasterMindAndPlanning())
        {
            if (allUserTargetsAreValid() && !pointingAtUI())
            {
                photonView.RPC("requestTeleport", RpcTarget.MasterClient, teleportMovement);
            }
            else
            {
                photonView.RPC("stopPlanning", RpcTarget.All);
            }

        }
    }

    // call in the frame that the cance button is pressed
    public virtual void cancelButtonPressed()
    {
        if (!pointingAtUI())
        {
            acceptProtocol.cancelButtonPressed();

            if (iAmMasterMindAndPlanning())
                photonView.RPC("stopPlanning", RpcTarget.All);
        }
    }

    // call in the frame that the cance button is released
    public virtual void cancelButtonReleased()
    {
        acceptProtocol.cancelButtonReleased();
    }

    #endregion

    #region VOTE RESPONSES
    void verdictCallback(VoteProtocol.Vote v)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (v == VoteProtocol.Vote.ACCEPT)
            {
                if (awaitFinalVerdict)
                {
                    this.photonView.RPC("executeTeleport", RpcTarget.All);
                }
            }
            else
            {
                if (!awaitFinalVerdict)
                {
                    cancelScheduledTeleport(TeleportStatus.INACTIVE);
                }
                else
                {
                    this.photonView.RPC("cancelTeleport", RpcTarget.All);
                }
            }
        }
    }

    // called when preliminary vote result is here (it can still change)
    void mutableCallback(VoteProtocol.Vote v)
    {
        if (PhotonNetwork.IsMasterClient && !awaitFinalVerdict)
        {
            if (v == VoteProtocol.Vote.ACCEPT)
            {
                scheduleTeleport();
            }
            else
            {
                cancelScheduledTeleport(TeleportStatus.REQUESTED);
            }
        }
    }

    // called when voting status changes to pending (e.g. from mutable)
    void pendingCallback()
    {
        if (PhotonNetwork.IsMasterClient && !awaitFinalVerdict)
        {
            cancelScheduledTeleport(TeleportStatus.REQUESTED);
        }
    }
    #endregion

    #region TELEPORT LOGIC

    [PunRPC]
    public void initiatePlanning(int plannerID)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (anyImplNowActive) return;
            photonView.RPC("initiatePlanning", RpcTarget.Others, plannerID);
        }

        MultiuserTeleport.anyImplNowActive = true;
        status = TeleportStatus.PLANNING;
        masterMindUser = plannerID;

        // update user relevance field so that rays are displayed correctly
        ensureUserTargetInfosIsUpToDate();
        foreach (var player in PhotonNetwork.PlayerList)
        {
            UserTargetInfo uti = findUserTargetInfo(player.ActorNumber);
            uti.prop.relevance = (plannerID == player.ActorNumber) ? UserRelevance.PLANNING_USER : UserRelevance.OTHER_USER;
            userTargetInfos[player.ActorNumber] = uti;
        }
    }

    [PunRPC]
    public void stopPlanning()
    {
        masterMindUser = -1;
        status = TeleportStatus.INACTIVE;
        MultiuserTeleport.anyImplNowActive = false;
    }

    [PunRPC]
    public void requestTeleport(Vector3 tpMovement)
    {
        // normal clients assume they get the message from master client and jsut accept it
        // master client first checks current state machine status, then approves request, starts vote and notifies other users
        if (PhotonNetwork.IsMasterClient)
        {
            if (acceptProtocol.GetVotingStage() != VoteProtocol.VotingStage.INACTIVE) return;   // only continue if not currently voting
            if (status != TeleportStatus.PLANNING) return;                                      // only continue if in phase planning
            acceptProtocol.startVote();
            this.photonView.RPC("requestTeleport", RpcTarget.Others, tpMovement);
        }

        teleportMovement = tpMovement;
        status = TeleportStatus.REQUESTED;
    }

    [PunRPC] // teleports immediately
    protected virtual void executeTeleport()
    {

        // do the actual teleporting
        VRRig.position += getVRRigMovementVector();

        // teleport any non user object that should also folow
        foreach (GameObject obj in nonUserTeleportObjects)
        {
            if (obj)
            {
                PhotonView pv = obj.GetComponent<PhotonView>();
                if (pv)
                {
                    if (pv.IsMine)
                    {    // if the object is shared only teleport if user is the owner
                        teleportNonUserObject(obj);
                    }
                }
                else
                {
                    teleportNonUserObject(obj);
                }
            }
        }

        // cleanup state
        status = TeleportStatus.INACTIVE;
        MultiuserTeleport.anyImplNowActive = false;
        masterMindUser = -1;

        // reset vote if master
        if (PhotonNetwork.IsMasterClient)
        {
            acceptProtocol.reset(true);
        }
    }

    // syncs timestamp at which master client will initiate a teleport
    void scheduleTeleport()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            teleportTimestamp = PhotonNetwork.Time + mutableVerdictCooldown;
            this.photonView.RPC("planTeleport", RpcTarget.All, teleportTimestamp, TeleportStatus.IMMINENT);
        }
    }

    // cancels an already scheduled teleport
    void cancelScheduledTeleport(TeleportStatus updatedStatus)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            teleportTimestamp = double.MaxValue;
            this.photonView.RPC("planTeleport", RpcTarget.All, teleportTimestamp, updatedStatus);   // plan teleport again but with no limit
        }
    }

    [PunRPC]
    public void cancelTeleport()
    {
        //Debug.Log("Canceling Teleport");
        teleportTimestamp = double.MaxValue;
        status = TeleportStatus.INACTIVE;

        MultiuserTeleport.anyImplNowActive = false;
        masterMindUser = -1;

        if (PhotonNetwork.IsMasterClient)
        {
            acceptProtocol.reset(true);
        }
    }

    [PunRPC] // sets timestamp for future teleport
    public void planTeleport(double timestamp, int updatedStatus)
    {
        //Debug.Log("Planning Teleport, new status: " + (TeleportStatus)updatedStatus);
        status = (TeleportStatus)updatedStatus;
        teleportTimestamp = timestamp;

        if (status == TeleportStatus.INACTIVE)
        {
            cancelTeleport();
        }
    }

    void teleportNonUserObject(GameObject obj)
    {
        Debug.Log("teleporting object " + obj.name);

        // determine objects height off the ground pre teleport
        const float searchDist = 50f;
        float before_ydist = 0;
        List<RaycastHit> hits = Physics.RaycastAll(obj.transform.position, Vector3.down, searchDist, FloorLayer).ToList();
        foreach (var hit in hits)
        {
            if (!hit.transform.IsChildOf(obj.transform))
            {
                if (hit.transform.tag == "Teleportable")
                {
                    before_ydist = hit.distance;
                    break;
                }
            }
        }
        hits = Physics.RaycastAll(obj.transform.position, Vector3.up, searchDist, FloorLayer).ToList();
        foreach (var hit in hits)
        {
            if (!hit.transform.IsChildOf(obj.transform))
            {
                if (hit.transform.tag == "Teleportable")
                {
                    before_ydist = -hit.distance;
                    break;
                }
            }
        }

        // set new object xz-position
        obj.transform.position += teleportMovement;

        // determine objects height off the ground post teleport
        float after_ydist = 0f;
        hits = Physics.RaycastAll(obj.transform.position, Vector3.down, searchDist, FloorLayer).ToList();
        foreach (var hit in hits)
        {
            if (!hit.transform.IsChildOf(obj.transform))
            {
                if (hit.transform.tag == "Teleportable")
                {
                    after_ydist = hit.distance;
                    break;
                }
            }
        }
        hits = Physics.RaycastAll(obj.transform.position, Vector3.up, searchDist, FloorLayer).ToList();
        foreach (var hit in hits)
        {
            if (!hit.transform.IsChildOf(obj.transform))
            {
                if (hit.transform.tag == "Teleportable")
                {
                    after_ydist = -hit.distance;
                    break;
                }
            }
        }

        // correct y distance
        float y_diff = after_ydist - before_ydist;
        obj.transform.position -= new Vector3(0, y_diff, 0);
    }

    #endregion

    #region VALIDATION
    protected bool allUserTargetsAreValid()
    {
        ensureUserTargetInfosIsUpToDate();
        foreach (var kvp in userTargetInfos)
        {
            if (!userTargetIsValid(kvp.Key, kvp.Value)) return false;
        }
        return true;
    }

    protected virtual bool userTargetIsValid(int actorNR, UserTargetInfo userTarget)
    {
        return !userTarget.tf.Find("Quad").GetComponent<CheckCollision>().CollidingWithOtherObject;
    }
    #endregion

    #region HELPER FUNCTIONS
    Material makeMaterialVariant(bool valid, Material validMRef, Material invalidMRef, Color c)
    {
        Material mat = new Material(valid ? validMRef : invalidMRef);
        mat.color = c;
        mat.SetColor("_EmissionColor", c);

        return mat;
    }

    protected Color getUserColor(int actorNR)
    {
        if (userColorAttribution.ContainsKey(actorNR))
        {
            return userColorList[userColorAttribution[actorNR]];
        }

        // prepare to create a new userColorAttribution dictionary
        userColorAttribution.Clear();
        List<Player> players = new List<Player>(PhotonNetwork.PlayerList);
        players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));  // sort players by actornumber

        int id = 0;
        foreach (var user in players)
        { // iterate over sorted player list
            userColorAttribution[user.ActorNumber] = id;
            id = (id + 1) % userColorList.Length;
        }

        if (userColorAttribution.ContainsKey(actorNR))
        {
            return userColorList[userColorAttribution[actorNR]];
        }

        throw new ArgumentException("Given actor number does not belong to any player: " + actorNR);
    }

    protected int myActornumber()
    {
        return PhotonNetwork.LocalPlayer.ActorNumber;
    }

    protected virtual string getUserTargetIndicatorName(int actorNR)
    {
        return "UserTargetIndicator" + actorNR;
    }

    protected bool iAmMasterMindAndPlanning()
    {
        return masterMindUser == myActornumber() && status == TeleportStatus.PLANNING;
    }

    protected bool pointingAtUI()
    {
        UserTargetInfo myTargetI = findUserTargetInfo(myActornumber());
        Transform userAvatar = myTargetI.avatarLHand.parent;
        Transform uiPointer = userAvatar.FindChildRecursive("GazeIcon");
        return uiPointer.gameObject.activeInHierarchy;
    }
    #endregion

    #region USER TARGET INFO SYNCHRONIZATION 
    // use this function to fetch information for one user out of the userTargetInfos dictionary
    protected UserTargetInfo findUserTargetInfo(int actorNR)
    {
        if (userTargetInfos.ContainsKey(actorNR)) return userTargetInfos[actorNR];

        // check if actorNR is even contained within playerlist
        Player userInQuestion = tryFindPlayer(actorNR);
        if (userInQuestion == null) throw new ArgumentException("FATAL: Given ActorNumber(" + actorNR + ") does not exist.");

        refreshUserTargetIndicators();

        // check if userTargetInfos now contains actorNR
        if (userTargetInfos.ContainsKey(actorNR)) return userTargetInfos[actorNR];

        // request that user instantiates their prefab

        photonView.RPC("instantiateUserTarget", userInQuestion, myActornumber());
        Debug.Log("Instantiating...802, userinquestion: "+ userInQuestion + ", actornumber: " + myActornumber());
        throw new Exception("Could not find userTargetInfo of user" + actorNR + ". Sent request to user so that they instantiate their userTarget. This should not happen again.");
    }

    // always call directly before iterating over userTargetInfos
    protected void ensureUserTargetInfosIsUpToDate()
    {
        if (PhotonNetwork.PlayerList.Length == userTargetInfos.Count) return;

        refreshUserTargetIndicators();

        if (PhotonNetwork.PlayerList.Length == userTargetInfos.Count) return;

        // find missing players
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!userTargetInfos.ContainsKey(player.ActorNumber))
            {
                photonView.RPC("instantiateUserTarget", player, myActornumber());
                Debug.Log("Instantiating...821" + player + ", actornumber: " + myActornumber());
                Debug.LogWarning("Could not find userTargetInfo of user" + player.ActorNumber + ". Sent request to user so that they instantiate their userTarget. This should not happen again.");
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        userTargetInfos.Remove(otherPlayer.ActorNumber);
    }

    [PunRPC]
    public void refreshUserTargetIndicators()
    {
        foreach (var user in PhotonNetwork.PlayerList)
        {
            if (!userTargetInfos.ContainsKey(user.ActorNumber))
            {
                Transform HMDTF = getAvatarPartTF(user.ActorNumber, "generic_hmd");
                Transform LeftTF = getAvatarPartTF(user.ActorNumber, "Left");
                Transform RightTF = getAvatarPartTF(user.ActorNumber, "Right");
                Transform Shirt = getAvatarPartTF(user.ActorNumber, "tshirt");

                GameObject userTarget = GameObject.Find(getUserTargetIndicatorName(user.ActorNumber));

                if (HMDTF && LeftTF && RightTF && userTarget)
                {
                    userTargetInfos[user.ActorNumber] = new UserTargetInfo(
                        HMDTF, LeftTF, RightTF, userTarget.transform, Shirt,
                        new UserTargetProperties(UserRelevance.OTHER_USER, validRayMaterial, validMarkerMaterial)
                    );
                }
                else
                {
                    Debug.LogWarning("The following parts could not be found for user " + user.NickName + " (ActorNumber=" + user.ActorNumber + ") (true=not here): (HMD, " + (HMDTF == null) + ");(LeftController, " + (LeftTF == null) + ");(RightController, " + (RightTF == null) + ");(target, " + (userTarget == null) + ")");
                }
            }
        }
        tintAvatarMaterial();
    }

    public void tintAvatarMaterial()
    {
        // do not call ensureUserTargetInfosIsUpToDate() here
        foreach (var kvp in userTargetInfos)
        {
            int actorNR = kvp.Key;
            UserTargetInfo userTargetI = kvp.Value;

            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            List<MeshRenderer> shirtRenderers = new List<MeshRenderer>();
            List<MeshRenderer> hmdRenderers = new List<MeshRenderer>();

            if (actorNR == myActornumber())
            {
                // NOTE to generalize this code find mesh renderers differently
                meshRenderers.AddRange(
                    VRRig.FindChildRecursive("LeftControllerAnchor")
                         .FindChildRecursive("OVRControllerPrefab")
                         .GetComponentsInChildren<MeshRenderer>()
                         .ToList()
                );
                meshRenderers.AddRange(
                    VRRig.FindChildRecursive("RightControllerAnchor")
                         .FindChildRecursive("OVRControllerPrefab")
                         .GetComponentsInChildren<MeshRenderer>()
                         .ToList()
                );
                shirtRenderers.AddRange(
                    userTargetI.shirt.GetComponentsInChildren<MeshRenderer>()
                );


                hmdRenderers.AddRange(
                    userTargetI.avatarHMD
                    .FindChildRecursive("generic_hmd_generic_hmd_mesh")
                    .GetComponentsInChildren<MeshRenderer>()
                    );
            }
            else
            {
                meshRenderers.AddRange(userTargetI.avatarLHand.GetComponentsInChildren<MeshRenderer>());
                meshRenderers.AddRange(userTargetI.avatarRHand.GetComponentsInChildren<MeshRenderer>());
                shirtRenderers.AddRange(userTargetI.shirt.GetComponentsInChildren<MeshRenderer>());
                hmdRenderers.AddRange(userTargetI.avatarHMD.GetComponentsInChildren<MeshRenderer>());
            }

            Material m = new Material(controllerTemplateMat);
            m.color = getUserColor(actorNR);
            m.SetColor("_EmissionColor", m.color * 0.3f);

            foreach (var mr in meshRenderers)
            {
                mr.material = m;
            }

            Material shirtM = new Material(controllerTemplateMat);
            shirtM.SetTexture("_MainTex", null);
            shirtM.color = getUserColor(actorNR);
            shirtM.SetColor("_EmissionColor", shirtM.color * 0.3f);

            foreach (var mr in shirtRenderers)
            {
                mr.material = shirtM;
            }

            foreach(var hr in hmdRenderers)
            {
                hr.material = shirtM;
            }
        }
    }

    protected Player tryFindPlayer(int actorNR)
    {
        try
        {
            return new List<Player>(PhotonNetwork.PlayerList).Find(c => c.ActorNumber == actorNR);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    [PunRPC]
    public void instantiateUserTarget(int requestingUser)
    {
        
        if ((requestingUser != -1 && userTargetInfos.ContainsKey(myActornumber())) || userTargetAlreadyInstantiated)
        {
            photonView.RPC("checkAgain", tryFindPlayer(requestingUser), myActornumber());
            return;
        }

        int actorNR = myActornumber();
        GameObject myTargetIndicator = PhotonNetwork.Instantiate("UserTargetIndicator", new Vector3(-10000, -10000, 0), Quaternion.identity);

        userTargetAlreadyInstantiated = true;

        myTargetIndicator.GetPhotonView().RPC("nameChange", RpcTarget.All, getUserTargetIndicatorName(actorNR));
        photonView.RPC("refreshUserTargetIndicators", RpcTarget.All);
    }

    [PunRPC]
    public void checkAgain(int respondingUser)
    {
        refreshUserTargetIndicators();
        if (!userTargetInfos.ContainsKey(respondingUser)) throw new Exception("FATAL at least in some cases our syncing method does not work (respondingUser=" + respondingUser + ").");
    }

    protected Transform getAvatarPartTF(int actorNR, string partName)
    {
        List<Player> players = new List<Player>(PhotonNetwork.PlayerList);
        Player user = players.Find(c => c.ActorNumber == actorNR);

        GameObject userAvatar = GameObject.Find(user.NickName);
        if (!userAvatar) return null;
        Transform avatarPart = userAvatar.transform.FindChildRecursive(partName);

        return avatarPart;
    }
    #endregion
}
