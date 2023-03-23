using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using Photon.Realtime;

// abstract class that allows multiple users to vote on a yes/no question
// the actual behavior is up to the implementation, the class just provides a framework to make swapping out multiple "vote protocols" as seamless as possible
// derived classes may use functions acceptButtonPressed(), acceptButtonReleased(), etc to implem,ent functionality, however entirely different methods may be chosen
// use sendVote() to share the voting decision of the client with other clients
// use updateVoteStatus() to decide the outcome and current stage of the voting process based on user votes
// voting outcomes can either be queried using GetVotingStage() & GetVerdict() or conveyed via callback functions on actions pendingCallback/mutableCallback/verdictCallback
public abstract class VoteProtocol : MonoBehaviourPunCallbacks
{
    public enum VotingStage {
        INACTIVE = 0,   // we do not vote currently
        PENDING  = 1,   // vote in progress, but undecided
        MUTABLE  = 2,   // preliminary verdict available but result can still change (e.g. users still have time to change their mind); state can also fall back to PENDING
        FINAL    = 3    // verdict is final, voting has concluded; outcome and RequestStatus do not chage until reset() is called
    }

    public enum Vote {
        PENDING = 0,    // awaiting decision
        DECLINE = 1,    
        ACCEPT  = 2
    }

    // synced variables
    public VotingStage voteStatus;
    public Dictionary<int, Vote> userStatus = new Dictionary<int, Vote>();
    protected Vote verdict;

    // unsynced; use to find difference between previous-, and current state within updateFeedback
    private VotingStage oldVoteStatus;
    private Dictionary<int, Vote> oldUserStatus = new Dictionary<int, Vote>();
    private Vote oldVerdict;

    public Action pendingCallback;
    public Action<Vote> mutableCallback;
    public Action<Vote> verdictCallback;

    void Start() {
        userStatus = new Dictionary<int, Vote>();
        verdict = Vote.PENDING;
        voteStatus = VotingStage.INACTIVE;

        oldUserStatus = rememberOldVotes();
        oldVerdict = verdict;
        oldVoteStatus = voteStatus;
    }

    void Update() {}

    public abstract void accceptButtonPressed();

    public abstract void acceptButtonReleased();

    public abstract void cancelButtonPressed();

    public abstract void cancelButtonReleased();

    // called on master client to determine new VotingStage and Verdict based on changed user votes
    protected abstract void updateVoteStatus();

    // called on every client when state changes allows stuff like hiding/unhiding ui elements based on state changes
    protected abstract void updateFeedback(VotingStage oldVoteStatus, Dictionary<int, Vote> oldUserStatus, Vote oldVerdict);

    protected void updateFeedback() {
        //Debug.Log("Updating Feedback, current Vote Status: " + voteStatus + "\nCurrent verdict: " + verdict);
        updateFeedback(oldVoteStatus, oldUserStatus, oldVerdict);

        if(oldVoteStatus != voteStatus) {
            switch(voteStatus) {
                case VotingStage.PENDING:
                    pendingCallback();
                    break;
                case VotingStage.MUTABLE:
                    mutableCallback(verdict);
                    break;
                case VotingStage.FINAL:
                    verdictCallback(verdict);
                    break;
            }
        }else if(voteStatus == VotingStage.MUTABLE && oldVerdict != verdict) {
            mutableCallback(verdict);
        } 

        oldUserStatus = rememberOldVotes();
        oldVerdict = verdict;
        oldVoteStatus = voteStatus;
    }

    [PunRPC]
    public void reset(bool force = false) {
        //Debug.Log("Reset Vote");
        if(!PhotonNetwork.IsMasterClient) {
            this.photonView.RPC("reset", RpcTarget.MasterClient, force);
        }else {
            if(force || voteStatus == VotingStage.FINAL) {
                voteStatus = VotingStage.INACTIVE;
                verdict = Vote.PENDING;
                prepareUserStatusDict();

                syncWithClients();

                updateFeedback();
            }
        }
    }

    [PunRPC]
    public void startVote() {
        //Debug.Log("Start Vote");
        if(!PhotonNetwork.IsMasterClient) {
            this.photonView.RPC("startVote", RpcTarget.MasterClient);
        }else {
            if(voteStatus == VotingStage.INACTIVE) {
                voteStatus = VotingStage.PENDING;
                prepareUserStatusDict();

                syncWithClients();

                updateFeedback();
            }
        }
    }

    void prepareUserStatusDict() {
         userStatus.Clear();
        foreach(Player player in PhotonNetwork.PlayerList) {
            userStatus[player.ActorNumber] = Vote.PENDING;
        }
    }

    // creates an RPC call to the vote master, telling them their new vote
    protected void sendVote(Vote v) {
        //Debug.Log("Send Vote" + v);
        if(voteStatus == VotingStage.PENDING || voteStatus == VotingStage.MUTABLE) {
            this.photonView.RPC("receiveVote", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, (int)v);
        }
    }

    [PunRPC] // the masterclient receives votes from other clients via rpc call, updates the vote status and synchronizes the result with all clients
    protected void receiveVote(int user, int vote) {
        //Debug.Log("Received Vote " + vote + " from user " + user);
        if(PhotonNetwork.IsMasterClient) {
            userStatus[user] = (Vote)vote;
            updateVoteStatus();
            syncWithClients();

            updateFeedback();
        }
    }

    // synchronizes the masters voting state with other clients
    protected void syncWithClients() {
        if(PhotonNetwork.IsMasterClient) {

            Dictionary<int,int> _userStatus = new Dictionary<int, int>{};
            foreach(var item in userStatus) {
                _userStatus[item.Key] = (int)item.Value;
            }

            this.photonView.RPC("distributeVoteStatus", RpcTarget.Others, (int)voteStatus, (int)verdict, _userStatus);
        }
    }

    [PunRPC]
    protected void distributeVoteStatus(int _voteStatus, int _verdict, Dictionary<int,int> _userStatus) {       
        voteStatus = (VotingStage)_voteStatus;
        verdict = (Vote)_verdict;
        userStatus.Clear();
        foreach(var item in _userStatus) {
            userStatus[item.Key] = (Vote)item.Value;
        }

        updateFeedback();
    }

    public Vote GetVerdict() {
        return verdict;
    }

    public VotingStage GetVotingStage() {
        return voteStatus;
    }

    Dictionary<int,Vote> rememberOldVotes() {
        return userStatus.ToDictionary(entry => entry.Key, entry => entry.Value);
    }


    // ------- MANAGE PLAYERS LEAVING AND ENTERING -------

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        if(PhotonNetwork.IsMasterClient) {
            userStatus[newPlayer.ActorNumber] = Vote.PENDING;
            syncWithClients();
            updateFeedback();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        if(PhotonNetwork.IsMasterClient) {
            userStatus.Remove(otherPlayer.ActorNumber);
            syncWithClients();
            updateFeedback();
        }
    }
}

//  -------  ENUM EXTENSIONS  ------- 

public static class VotingExtensions {
    public static bool voteInProgress(this VoteProtocol.VotingStage v) {
        return v == VoteProtocol.VotingStage.PENDING || v == VoteProtocol.VotingStage.MUTABLE;
    }

    public static bool decisionMade(this VoteProtocol.Vote v) {
        return v != VoteProtocol.Vote.PENDING;
    }
}