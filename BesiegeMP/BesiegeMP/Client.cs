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
    class Client : MonoBehaviour
    {
        private int reliableChannelId, chatChannelId, importantChannelId, hostId, connectionId, sPort;
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
        private Dictionary<String, Action> Actions = new Dictionary<string, Action>();

        public void Start()
        {
            NetworkTransport.Init();
            ConnectionConfig config = new ConnectionConfig();
            reliableChannelId = config.AddChannel(QosType.Reliable);
            chatChannelId = config.AddChannel(QosType.Unreliable);
            importantChannelId = config.AddChannel(QosType.AllCostDelivery);
            topology = new HostTopology(config, Settings.maxPlayers);
            text = gameObject.GetComponent<GeneralGUI>().text;
            messages.Add("");
        }

        public void Connect()
        {
            byte error;
            connectionId = NetworkTransport.Connect(hostId, sAdress, sPort, 0, out error);
        }

        public void Disconnect()
        {
            byte error;
            NetworkTransport.Disconnect(hostId, connectionId, out error);
        }

        public void SendChatMessage(String message)
        {
            SendMessage(message, chatChannelId);
            messages.Add(Settings.Name + ":" + message);
        }

        public void Update()
        {
            int outconnectionId;
            int outchannelId;
            byte[] buffer = new byte[1024];
            int recSize;
            byte error;
            NetworkEventType recData = NetworkTransport.ReceiveFromHost(hostId, out outconnectionId, out outchannelId, buffer, 1024, out recSize, out error);

            switch (recData)
            {
                case NetworkEventType.Nothing:
                {
                    break;
                }
                case NetworkEventType.ConnectEvent:
                {
                    if (connectionId == outconnectionId)
                    {
                        SendMessage("Name:" + Settings.Name, reliableChannelId);
                    }
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

        private void SendMessage(String message, int channelId)
        {
            byte[] buff = Encoding.Unicode.GetBytes(message.ToCharArray());
            byte err;
            NetworkTransport.Send(hostId, connectionId, channelId, buff, buff.Length, out err);
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
