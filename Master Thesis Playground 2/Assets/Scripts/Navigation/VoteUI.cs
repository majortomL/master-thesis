using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

using Vote = VoteProtocol.Vote;
using VotingStage = VoteProtocol.VotingStage;

public class VoteUI : MonoBehaviour
{
    public string voteAcceptText = "ACCEPT";
    public string voteDeclineText = "DECLINE";
    public string votePendingText = "UNDECIDED";

    public string statusPendingText = "VOTE ONGOING";
    public string statusMutableText = "PRELIMINARY RESULT";
    public string statusFinalText = "VOTE CLOSED";

    private Canvas canvas;

    private Text title;
    private Text outcome;
    private Text userVote;
    private Text status;
    private Text timer;

    private Image outcomeYes;
    private Image outcomeNo;
    private Image outcomeWait;

    private Image voteYes;
    private Image voteNo;
    private Image voteWait;

    private Slider yesSlider;
    private Slider noSlider;

    private double decisionTimestamp = -1;

    // Start is called before the first frame update
    void Start()
    {
        canvas      = transform.GetComponent<Canvas>();

        title       = transform.FindChildRecursive("VoteTitle").GetComponent<Text>();
        outcome     = transform.FindChildRecursive("OutcomeText").GetComponent<Text>();
        userVote    = transform.FindChildRecursive("VoteText").GetComponent<Text>();
        status      = transform.FindChildRecursive("StatusText").GetComponent<Text>();
        timer       = transform.FindChildRecursive("TimerText").GetComponent<Text>();

        outcomeYes  = transform.FindChildRecursive("OutcomeAccept").GetComponent<Image>();
        outcomeNo   = transform.FindChildRecursive("OutcomeDecline").GetComponent<Image>();
        outcomeWait = transform.FindChildRecursive("OutcomePending").GetComponent<Image>();

        voteYes     = transform.FindChildRecursive("VoteAccept").GetComponent<Image>();
        voteNo      = transform.FindChildRecursive("VoteDecline").GetComponent<Image>();
        voteWait    = transform.FindChildRecursive("VotePending").GetComponent<Image>();

        yesSlider   = transform.FindChildRecursive("SliderAccept").GetComponent<Slider>();
        noSlider    = transform.FindChildRecursive("SliderDecline").GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        double remainingTime = decisionTimestamp - PhotonNetwork.Time;
        if(remainingTime < 0.0f) {
            timer.text = "";
        }else {
            TimeSpan t = TimeSpan.FromSeconds(Mathf.Min((float)remainingTime, 60 * 60 * 24));
            if(t >= TimeSpan.FromHours(1)) {
                timer.text = " \u221e:  \u221e";
            }else {
                timer.text = t.ToString("mm\\:ss");
            }
        }
    }

    public void modify(string voteTitle, VotingStage stage, Vote verdict, Vote myVote, int userCount, int voteCountYes, int voteCountNo, double _decisionTimestamp) {
        if(stage == VotingStage.INACTIVE) {
            canvas.enabled = false;

        }else {
            canvas.enabled = true;

            title.text = voteTitle;

            resetImages();

            // set outcome results
            switch(verdict) {
                case Vote.ACCEPT:
                    outcome.text = voteAcceptText;
                    outcomeYes.enabled = true;
                    break;
                case Vote.DECLINE:
                    outcome.text = voteDeclineText;
                    outcomeNo.enabled = true;
                    break;
                case Vote.PENDING:
                    outcome.text = votePendingText;
                    outcomeWait.enabled = true;
                    break;
            }

            // set user vote results
            switch(myVote) {
                case Vote.ACCEPT:
                    userVote.text = voteAcceptText;
                    voteYes.enabled = true;
                    break;
                case Vote.DECLINE:
                    userVote.text = voteDeclineText;
                    voteNo.enabled = true;
                    break;
                case Vote.PENDING:
                    userVote.text = votePendingText;
                    voteWait.enabled = true;
                    break;
            }

            // set vote status
            switch(stage) {
                case VotingStage.PENDING:
                    status.text = statusPendingText;
                    break;
                case VotingStage.MUTABLE:
                    status.text = statusMutableText;
                    break;
                case VotingStage.FINAL:
                    status.text = statusFinalText;
                    break;
            }

            // set slider states
            yesSlider.maxValue = userCount;
            noSlider.maxValue = userCount;
            yesSlider.value = voteCountYes;
            noSlider.value = voteCountYes + voteCountNo;
            // make slider bodies invisible if they amount to nothing (otherwise the bar remains filled a tiny bit)
            yesSlider.fillRect.gameObject.SetActive(voteCountYes != 0);
            noSlider.fillRect.gameObject.SetActive(voteCountNo != 0);

            decisionTimestamp = _decisionTimestamp;
        }
    }

    void resetImages() {
        outcomeYes.enabled  = false;
        outcomeNo.enabled   = false;
        outcomeWait.enabled = false;

        voteYes.enabled     = false;
        voteNo.enabled      = false;
        voteWait.enabled    = false;
    }
}
