using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class ContinuousVote : VoteProtocol
{
    public string title = "Vote";

    public float acceptRate = 1.0f;
    public float acceptCooldown = 3.0f;

    public VoteUI voteUI;

    double decisionTimestamp = double.MaxValue;

    VibrationPattern genericFeedback = VibrationPattern.makeNumbered(0.6f, 1, 1, 0.1f, 1, 1, 0);
    VibrationPattern notifyPending = VibrationPattern.makeNumbered(0.6f, 1, 1, 2, 2, 0.2f, 0.9f);
    VibrationPattern remindPending = VibrationPattern.makeUnrestricted(0.4f, 1, 2, 2, 0.2f, 0.9f);
    VibrationPattern notifyVerdict = VibrationPattern.makeNumbered(1, 1, 1, 0.5f, 1, 1);

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (voteStatus)
        {
            case VotingStage.MUTABLE:
                if (PhotonNetwork.Time >= decisionTimestamp)
                {
                    voteStatus = VotingStage.FINAL;
                    syncWithClients();
                    updateFeedback();
                    decisionTimestamp = double.MaxValue;
                }
                break;
        }
    }

    public override void accceptButtonPressed()
    {
        sendVote(Vote.ACCEPT);
    }

    public override void acceptButtonReleased()
    {
        sendVote(Vote.PENDING);
    }

    public override void cancelButtonPressed()
    {
        sendVote(Vote.DECLINE);
    }

    public override void cancelButtonReleased()
    {
        // nothing should happen
    }

    // called on master client to determine new VotingStage and Verdict based on changed user votes
    protected override void updateVoteStatus()
    {
        switch (voteStatus)
        {
            case VotingStage.PENDING:
            case VotingStage.MUTABLE:
                voteLogic();
                break;
        }
    }

    void voteLogic()
    {
        // loop over user votes; count accepts; one decline automatically rejects vote
        float acceptCount = 0;
        foreach (var uservote in userStatus)
        {
            if (uservote.Value == Vote.DECLINE)
            {
                verdict = Vote.DECLINE;
                voteStatus = VotingStage.FINAL;
                return;
            }
            if (uservote.Value == Vote.ACCEPT)
            {
                acceptCount++;
            }
        }

        // if accept count over threshold 
        if (acceptCount >= (float)userStatus.Count * acceptRate)
        {
            verdict = Vote.ACCEPT;
            if (voteStatus == VotingStage.PENDING)
            {
                decisionTimestamp = PhotonNetwork.Time + acceptCooldown;
                voteStatus = VotingStage.MUTABLE;
            }
        }
        else
        { // accept count below threshold
            verdict = Vote.PENDING;
            voteStatus = VotingStage.PENDING;
            decisionTimestamp = double.MaxValue;
        }
    }

    // called on every client when state changes allows stuff like hiding/unhiding ui elements based on state changes
    protected override void updateFeedback(VotingStage oldVoteStatus, Dictionary<int, Vote> oldUserStatus, Vote oldVerdict)
    {
        int myUser = PhotonNetwork.LocalPlayer.ActorNumber;
        Vote myOldVote = oldUserStatus.ContainsKey(myUser) ? oldUserStatus[myUser] : Vote.PENDING;
        Vote myVote = userStatus.ContainsKey(myUser) ? userStatus[myUser] : Vote.PENDING;

        if (!oldVoteStatus.voteInProgress() && voteStatus.voteInProgress())
        {
            VibrationHandler.startPattern(notifyPending);
            VibrationHandler.startPattern(remindPending);
        }
        if (oldVoteStatus.voteInProgress() && !voteStatus.voteInProgress())
        {
            VibrationHandler.stopPattern(remindPending);
            VibrationHandler.startPattern(notifyVerdict);
        }

        if (voteStatus.voteInProgress())
        {
            if (!myOldVote.decisionMade() && myVote.decisionMade())
            {
                VibrationHandler.stopPattern(remindPending);
                VibrationHandler.startPattern(genericFeedback);
            }

            if (myOldVote.decisionMade() && !myVote.decisionMade())
            {
                VibrationHandler.startPattern(remindPending);
                VibrationHandler.startPattern(genericFeedback);
            }
        }

        if (voteUI)
        {
            int acceptCount = 0;
            int declineCount = 0;
            foreach (var userVote in userStatus)
            {
                switch (userVote.Value)
                {
                    case Vote.ACCEPT:
                        acceptCount++;
                        break;
                    case Vote.DECLINE:
                        declineCount++;
                        break;
                }

            }
            voteUI.modify(title, voteStatus, verdict, myVote, userStatus.Count, acceptCount, declineCount, decisionTimestamp);
        }

        // if INACTIVE => PENDING unhide ui & start vibration feedback
        // if __ => INACTIVE hide ui

        // if (PENDING || MUTABLE && userVote: __ => PENDING) start vibration feedback
        // general feedback stuff
    }
}
