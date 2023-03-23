using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Text;
using System;
using Photon.Realtime;

public class DataCollector : MonoBehaviourPunCallbacks
{
    public string CSVPath = @"F:\BimFlexi\bimflexi\DataRecording";
    public bool ActivateDataCollection = false;

    // Teleporting is indexed as follows:
    // 0: Nothing has happened
    // 1: This player is planning
    // 2: This player has initiated voting
    // 3: Voting is ongoing
    // 4: Player has voted yes
    // 5: This player did teleport
    public Dictionary<int, Tuple<int, int>> teleportStatusLastPeriod = new Dictionary<int, Tuple<int, int>>();
    public Dictionary<int, bool> modelTeleportPlanning = new Dictionary<int, bool>();
    public ContinuousVote voteParabola;
    public ContinuousVote voteModel;
    public ParabolaTeleport parabolaTeleport;
    public InterCubeTeleport modelTeleport;

    private string CSVName;
    private bool dataCollectionActivated = false;
    private DateTime startOfCollection;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // We hit the tick last frame
        if (ActivateDataCollection && !dataCollectionActivated)
        {
            dataCollectionActivated = true;
            startOfCollection = DateTime.Now;

            // Initiate the teleport status dictionary with 0 for every player
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                teleportStatusLastPeriod.Add(player.ActorNumber, new Tuple<int, int>(0, 0));
                modelTeleportPlanning.Add(player.ActorNumber, false);
            }

            initiateCSV();
            InvokeRepeating("collectData", 0.0f, 1.0f);
        }
    }

    private void initiateCSV()
    {
        // Data Recording is only performed on the master client
        if (PhotonNetwork.IsMasterClient)
        {
            DateTime now = DateTime.Now;
            CSVName = now.ToString("yyyy") + "-" + now.ToString("MM") + "-" + now.ToString("dd") + "T" + now.ToString("HH") + "-" + now.ToString("mm") + "-" + now.ToString("ss") + ".csv";

            string playerNameString = "";
            string headerString = "";

            using (FileStream fileStream = File.Create(CSVPath + @"\" + CSVName))
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    playerNameString += "Player Nickname: " + player.NickName + "; Player ID: " + player.ActorNumber + new string(';', 7) + new string(';', (System.Int32)PhotonNetwork.CountOfPlayers);
                    //headerString += "Time; Player Name; Position X; Position Y; Position Z;";

                    // Time | Name | X | Y | Z | Parabola | Model | Distances...
                    headerString += "Time; Player " + player.ActorNumber + " Name; Player " + player.ActorNumber + " Position X; Player " + player.ActorNumber + " Position Y; Player " + player.ActorNumber + " Position Z; Player " + player.ActorNumber + " Parabola Teleport; Player " + player.ActorNumber + " Model Teleport;";

                    foreach (Player otherPlayer in PhotonNetwork.PlayerList)
                    {
                        if (otherPlayer == player)
                        {
                            continue;
                        }
                        else
                        {
                            headerString += "Player " + player.ActorNumber + " Distance to Player " + otherPlayer.ActorNumber + ";";
                        }
                    }

                    headerString += ";";
                }

                playerNameString += Environment.NewLine;
                headerString += Environment.NewLine;

                byte[] playerName = new UTF8Encoding(true).GetBytes(playerNameString);
                fileStream.Write(playerName, 0, playerName.Length);
                byte[] header = new UTF8Encoding(true).GetBytes(headerString);
                fileStream.Write(header, 0, header.Length);
            }
        }
    }

    private void collectData()
    {
        // === DEBUGGING COMMANDS ===
        //foreach (KeyValuePair<int, VoteProtocol.Vote> kvp in voteParabola.userStatus) // int is the actor number here
        //{
        //    Debug.Log("asdf");
        //    Debug.Log(kvp.Key + ":" + kvp.Value);
        //}
        //Debug.Log(voteParabola.userStatus.Count);
        //Debug.Log(voteParabola.userStatus);
        //Debug.Log(voteModel.userStatus);

        bool parabolaTeleportHappened = false;
        bool modelTeleportHappened = false;
        bool parabolaTeleportRequested = false;
        bool modelTeleportRequested = false;


        foreach (KeyValuePair<int, Tuple<int, int>> keyValuePair in teleportStatusLastPeriod)
        {
            // Check if any teleports happened
            if (keyValuePair.Value.Item1 == 5)
            {
                parabolaTeleportHappened = true;
            }

            if (keyValuePair.Value.Item2 == 5)
            {
                modelTeleportHappened = true;
            }

            if (keyValuePair.Value.Item1 == 2)
            {
                parabolaTeleportRequested = true;
            }

            if (keyValuePair.Value.Item2 == 2)
            {
                modelTeleportRequested = true;
            }
        }

        Vector3 position;
        float distance;

        // Collect current Time
        int time = Convert.ToInt32((DateTime.Now - startOfCollection).TotalSeconds);

        // Placeholder for the string of all players
        string dataString = "";

        // Go through all players
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
            GameObject playerGameObject = null;

            // Find our own Game Object
            foreach (GameObject playerObject in playerObjects)
            {
                if (playerObject.name == player.NickName)
                {
                    playerGameObject = playerObject;
                    break;
                }
            }

            // === Position Recording ===
            // Record our own position and add it to the data string
            // Time | Name | X | Y | Z | 
            position = playerGameObject.transform.Find("SimpleQuestCharacter").transform.Find("Dragon").transform.position;
            dataString += time.ToString() + ";" + player.NickName + ";" + position.x + ";" + position.y + ";" + position.z + ";";


            // === Teleport Recording === 
            // Time | Name | X | Y | Z | Parabola | Model |
            // Teleporting is indexed as follows:
            // 1: This player is planning
            // 2: This player has requested a teleport
            // 3: This player is voting
            // 4: This player voted yes
            // 5: This player did teleport - supported

            if (parabolaTeleportRequested)
            {
                dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
            }
            else if (modelTeleportRequested)
            {
                dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
            }
            else if (parabolaTeleportHappened) // if a parabola teleport happened, all players get a value 5
            {
                dataString += 5.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
            }
            else if (modelTeleportHappened) // if a model teleport happened, all players get a value 5
            {
                dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + 5.ToString() + ";";
            }
            else if (parabolaTeleport.status == MultiuserTeleport.TeleportStatus.PLANNING && parabolaTeleport.masterMindUser == player.ActorNumber) // We are planning a parabola teleport
            {
                dataString += 1.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
            }
            else if (voteParabola.voteStatus == VoteProtocol.VotingStage.PENDING || voteParabola.voteStatus == VoteProtocol.VotingStage.MUTABLE) // We are voting on a parabola teleport
            {
                if (voteParabola.userStatus[player.ActorNumber] == VoteProtocol.Vote.ACCEPT)
                {
                    dataString += 4.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
                }
                else
                {
                    dataString += 3.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
                }
            }
            else if (voteModel.voteStatus == VoteProtocol.VotingStage.PENDING || voteModel.voteStatus == VoteProtocol.VotingStage.MUTABLE) // We are voting on a model teleport
            {
                if (voteModel.userStatus[player.ActorNumber] == VoteProtocol.Vote.ACCEPT)
                {
                    dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + 4.ToString() + ";";
                }
                else
                {
                    dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + 3.ToString() + ";";
                }
            }
            else if (modelTeleportPlanning[player.ActorNumber])
            {
                dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + 1.ToString() + ";";
            }
            else
            {
                dataString += teleportStatusLastPeriod[player.ActorNumber].Item1.ToString() + ";" + teleportStatusLastPeriod[player.ActorNumber].Item2.ToString() + ";";
            }

            // Iterate through all other players
            foreach (Player otherPlayer in PhotonNetwork.PlayerList)
            {
                if (otherPlayer == player)
                {
                    continue;
                }
                else
                {
                    // Calculate the distance from our player
                    distance = Vector3.Distance(GameObject.Find(otherPlayer.NickName).transform.Find("SimpleQuestCharacter").transform.Find("Dragon").transform.position, position);
                    // Time | Name | X | Y | Z | Parabola | Model | Distances...
                    dataString += distance.ToString() + ";";
                }
            }
            dataString += ";";
        }
        dataString += Environment.NewLine;
        writeToCSV(dataString);

        // Reset our teleport monitor dictionary
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (teleportStatusLastPeriod[player.ActorNumber].Item2 != 1)
            {
                teleportStatusLastPeriod[player.ActorNumber] = new Tuple<int, int>(0, 0);
            }
        }
    }

    private void writeToCSV(string data)
    {
        File.AppendAllText(CSVPath + @"\" + CSVName, data);
    }

    [PunRPC]
    private void updateTeleportStatus(int actorNr, int parabolaStatus, int modelStatus, PhotonMessageInfo messageInfo)
    {
        if (dataCollectionActivated)
        {
            teleportStatusLastPeriod[messageInfo.Sender.ActorNumber] = new Tuple<int, int>(parabolaStatus, modelStatus);

            // Just for testing
            foreach (KeyValuePair<int, Tuple<int, int>> keyValuePair in teleportStatusLastPeriod)
            {
                Debug.Log("Key: " + keyValuePair.Key + ", Value: " + keyValuePair.Value.Item1 + ";" + keyValuePair.Value.Item2);
            }
        }
    }

    [PunRPC]
    private void updateModelPlanning(bool planning, PhotonMessageInfo messageInfo)
    {
        if (dataCollectionActivated)
        {
            modelTeleportPlanning[messageInfo.Sender.ActorNumber] = planning;
        }
    }
}
