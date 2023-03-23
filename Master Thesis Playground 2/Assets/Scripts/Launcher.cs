/*
 * Copyright (c) 2019 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;

namespace Photon.Pun.Demo.PunBasics
{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        [SerializeField]
        private GameObject controlPanel;

        [SerializeField]
        private Text feedbackText;

        [SerializeField]
        private byte maxPlayersPerRoom = 2;

        bool isConnecting;

        string gameVersion = "1";


        [Space(5)]
        public Text playerStatus;
        public Text connectionStatus;

        [Space(5)]
        public GameObject roomJoinUI;
        public GameObject buttonLoadArena;
        public GameObject buttonJoinRoom;

        public string playerName = "";
        public string roomName = "";

        public bool playOnDesktop;

        [Tooltip("Scene Index for the user study: " +
            "0: Default Testing Scene, " +
            "1: Training Scene Parabola, " +
            "2: Training Scene Inter Cube, " +
            "3: Training Scene Blueprint, " +
            "4: Study Scene Parabola, " +
            "5: Study Scene Intercube, " +
            "6: Study Scene both Methods, " +
            "7: Study Scene with Indicators"
            )]

        [Range(0, 7)]
        public int sceneIndex = 0;

        public Dictionary<int, string> sceneDictionary = new Dictionary<int, string>()
        {
            {0, "TestScene" },
            {1, "Training Scene Parabola" },
            {2, "Training Scene InterCube" },
            {3, "Training Scene Blueprint" },
            {4, "Study Scene Parabola" },
            {5, "Study Scene Intercube" },
            {6, "Study Scene both Methods" },
            {7, "Study Scene with Indicators" }
        };

        private Dictionary<int, string> playerNames = new Dictionary<int, string>();
        private bool started = false;
        // Start Method
        void Start()
        {
            // Setup of player names for user study
            playerNames[0] = "Red";
            playerNames[1] = "Blue";
            playerNames[2] = "Green";
            playerNames[3] = "Yellow";
            playerNames[4] = "User4";

            //1
            PlayerPrefs.DeleteAll();

            //Debug.Log("Connecting to Photon Network");

            //2
            roomJoinUI.SetActive(false);
            buttonLoadArena.SetActive(false);

            //3
            ConnectToPhoton();

            GameObject eventSystem = GameObject.Find("EventSystem");
            GameObject inputCanvas = GameObject.Find("Canvas");
            Canvas canvas = inputCanvas.GetComponent<Canvas>();

            if (playOnDesktop)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                //eventSystem.GetComponent<OVRInputModule>().enabled = false;
                //inputCanvas.GetComponent<OVRRaycaster>().enabled = false;
            }
            else
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;

                inputCanvas.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
                Debug.Log(Camera.main);
                inputCanvas.transform.position = Camera.main.transform.position + new Vector3(0.0f, 0.0f, 2.0f);

                //eventSystem.GetComponent<StandaloneInputModule>().enabled = false;
                //eventSystem.GetComponent<BaseInput>().enabled = false;
                //inputCanvas.GetComponent<GraphicRaycaster>().enabled = false;
            }
        }

        private void Update()
        {
            // Not needed - the master just starts the session manually!
            //if (PhotonNetwork.CountOfPlayers >= 2 && PhotonNetwork.MasterClient.NickName == PhotonNetwork.LocalPlayer.NickName && !started)
            //{
            //    LoadArena();
            //    started = false;
            //}
        }

        void Awake()
        {
            //4 
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        // Helper Methods
        public void SetPlayerName(string name)
        {
            playerName = name;
        }

        public void SetRoomName(string name)
        {
            roomName = name;
        }

        // Tutorial Methods
        void ConnectToPhoton()
        {
            connectionStatus.text = "Connecting...";
            PhotonNetwork.GameVersion = gameVersion; //1
            PhotonNetwork.ConnectUsingSettings(); //2
        }

        public void JoinRoom()
        {
            if (PhotonNetwork.IsConnected)
            {
                //Debug.Log("PhotonNetwork.IsConnected! | Trying to Create/Join Room ");
                RoomOptions roomOptions = new RoomOptions(); //2

                PhotonNetwork.AuthValues = new AuthenticationValues();
                PhotonNetwork.AuthValues.UserId = playerName;

                TypedLobby typedLobby = new TypedLobby(roomName, LobbyType.Default); //3
                PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, typedLobby); //4
            }
        }

        public void LoadArena()
        {
            // 5
            Debug.Log(PhotonNetwork.CurrentRoom);
            if (PhotonNetwork.CurrentRoom.PlayerCount > 0)
            {
                PhotonNetwork.LoadLevel(sceneDictionary[sceneIndex]);
            }
            else
            {
                playerStatus.text = "Minimum 2 Players required to Load Arena!";
            }
        }

        // Photon Methods
        public override void OnConnected()
        {
            Debug.Log("Connected");
            // 1
            base.OnConnected();
            // 2
            connectionStatus.text = "Connected to Photon!";
            connectionStatus.color = Color.green;
            roomJoinUI.SetActive(true);
            buttonLoadArena.SetActive(false);

        }

        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            JoinRoom();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            // 3
            isConnecting = false;
            controlPanel.SetActive(true);
            Debug.LogError("Disconnected. Please check your Internet connection.");
        }
        
        public override void OnJoinedRoom()
        {
            // Set this name fixed
            playerName = playerNames[PhotonNetwork.CurrentRoom.PlayerCount - 1];
            PhotonNetwork.LocalPlayer.NickName = playerName;

            //Debug.Log("Joined Voice, your user name is " + PhotonNetwork.LocalPlayer.UserId);

            PhotonNetwork.AuthValues = new AuthenticationValues();
            PhotonNetwork.AuthValues.UserId = playerName;

            //VoiceNetwork.ConnectAndJoinRoom();


            // 4
            if (PhotonNetwork.IsMasterClient)
            {
                buttonLoadArena.SetActive(true);
                buttonJoinRoom.SetActive(false);
                playerStatus.text = "Your are Lobby Leader";
            }
            else
            {
                playerStatus.text = "Connected to Lobby";
            }

            LoadArena();
        }
    }
}
