using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using spaar.ModLoader;
using spaar.ModLoader.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace BesiegeMP
{
    class Server : MonoBehaviour
    {
        private int miscChannelId, chatChannelId, importantChannelId, blockChannelId, socketID, connectionId, sPort;
        private HostTopology topology;
        private String sAdress;
        private Key show = Keybindings.AddKeybinding("Chat", new Key(KeyCode.LeftControl, KeyCode.Q));
        private GUIStyle windowStyle;
        private bool showWin, stylesDone;
        private int _winId = spaar.ModLoader.Util.GetWindowID();
        private Rect winRect = new Rect(0.0f, Screen.height - 300.0f, 400.0f, 300.0f);
        private List<String> messages = new List<string>();
        private String message = "";
        private Texture2D text;
        private Vector2 pos;
        private Dictionary<String, Dictionary<String, Component>> Users = new Dictionary<string, Dictionary<string, Component>>();
        private Dictionary<String, Action> Actions = new Dictionary<string, Action>();

        public void Start()
        {
            NetworkTransport.Init();
            ConnectionConfig config = new ConnectionConfig();
            miscChannelId = config.AddChannel(QosType.Reliable);
            blockChannelId = config.AddChannel(QosType.Reliable);
            chatChannelId = config.AddChannel(QosType.Unreliable);
            importantChannelId = config.AddChannel(QosType.AllCostDelivery);
            topology = new HostTopology(config, Settings.maxPlayers);
            text = gameObject.GetComponent<GeneralGUI>().text;
            messages.Add("");
        }

        public void StartHost()
        {
            socketID = NetworkTransport.AddHost(topology, Settings.ServerPort);
        }

        public void StopHost()
        {
            NetworkTransport.RemoveHost(socketID);
        }

        public void SendChatMessage(String message)
        {
            SendMessage(message, chatChannelId, connectionId);
            messages.Add(Settings.Name + ":" + message);
        }

        public void Update()
        {
            int outconnectionId;
            int outchannelId;
            byte[] buffer = new byte[1024];
            int recSize;
            int id;
            byte error;
            NetworkEventType recData = NetworkTransport.Receive(out id, out outconnectionId, out outchannelId, buffer, 1024, out recSize, out error);

            switch (recData)
            {
                case NetworkEventType.Nothing:
                    {
                        break;
                    }
                case NetworkEventType.ConnectEvent:
                    {
                        break;
                    }
                case NetworkEventType.DisconnectEvent:
                    {
                        if (outconnectionId == connectionId)
                        {
                            //Revert changes done while playing
                        }
                        break;
                    }
                case NetworkEventType.DataEvent:
                    {
                        if (outchannelId == chatChannelId)
                        {
                            messages.Add(Encoding.Unicode.GetString(buffer));
                            break;
                        }
                        if (outchannelId == miscChannelId)
                        {
                            String message = Convert(buffer);
                            if (message.StartsWith("Name:"))
                            {
                                Users.Add(message.Replace("Name:", ""), new Dictionary<string, Component>() { {"AddPiece", gameObject.AddComponent<AddPiece>()}, {"Machine", gameObject.AddComponent<Machine>()}, {"MachineObjectTracker", gameObject.AddComponent<MachineObjectTracker>()} });
                                
                                break;
                            }
                        }
                        //handle info for blocks etc

                        break;
                    }
            }



            if (show.Pressed())
            {
                showWin = !showWin;
            }
        }

        private String Convert(byte[] buffer)
        {
            return Encoding.Unicode.GetString(buffer);
        }

        private byte[] Convert(String buff)
        {
            return Encoding.Unicode.GetBytes(buff);
        }

        private void SendMessage(String message, int channelId, int connectionID)
        {
            byte[] buff = Convert(message);
            byte err;
            NetworkTransport.Send(socketID, connectionID, channelId, buff, buff.Length, out err);
        }

        public void OnGUI()
        {
            if (showWin)
            {
                if (!stylesDone)
                {
                    GUI.skin = ModGUI.Skin;
                    windowStyle = new GUIStyle(GUI.skin.window)
                    {
                        normal = new GUIStyleState()
                        {
                            background = text,
                            textColor = GUI.skin.window.normal.textColor
                        }
                    };
                    stylesDone = true;
                }
                GUI.skin = ModGUI.Skin;
                winRect = GUI.Window(_winId, winRect, WinFunc, "Chat", windowStyle);
            }
        }

        private void WinFunc(int id)
        {
            if (messages.Count > 0)
            {
                pos = GUILayout.BeginScrollView(pos);
                foreach (string s in messages)
                {
                    GUILayout.Label(s);
                }
                GUILayout.EndScrollView();
            }

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "chatInput" && Regex.IsMatch(message, @"[^\s]"))
            {
                SendChatMessage(message);
                message = "";
            }

            GUI.SetNextControlName("chatInput");
            message = GUILayout.TextField(message);
            GUI.DragWindow();
        }
    }
}
