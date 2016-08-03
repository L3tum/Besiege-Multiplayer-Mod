#region usings

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BesiegeMP.VoiceChat.Scripts;
using spaar.ModLoader;
using spaar.ModLoader.UI;
using UnityEngine;
using UnityEngine.Networking;
using VoiceChat;

#endregion

namespace BesiegeMP.Network
{
    internal class Network : MonoBehaviour
    {
        private readonly int _winId = spaar.ModLoader.Util.GetWindowID();
        private string message = "";
        public readonly List<string> messages = new List<string>();
        private Vector2 pos;
        private string sAdress;
        private readonly Key show = Keybindings.AddKeybinding("Chat", new Key(KeyCode.LeftControl, KeyCode.Q));
        private bool showWin, stylesDone, isServer;
        private Texture2D text;
        private GUIStyle windowStyle;
        private Rect winRect = new Rect(0.0f, Screen.height - 500.0f, 400.0f, 300.0f);
        public NetworkThread networkThread;
        private Thread thread, thread2;
        public List<int> usersToGameObjects = new List<int>();
        public List<VoiceChatPacket> vcps = new List<VoiceChatPacket>(); 

        delegate String responseDelegate(String message);

        public void Start()
        {
            networkThread = new NetworkThread(Settings.ticks) {network = this};
            NetworkTransport.Init();
            ConnectionConfig config = new ConnectionConfig();
            networkThread.miscChannelId = config.AddChannel(QosType.Reliable);
            networkThread.blockChannelId = config.AddChannel(QosType.ReliableStateUpdate);
            networkThread.chatChannelId = config.AddChannel(QosType.Unreliable);
            networkThread.methodChannelId = config.AddChannel(QosType.Reliable);
            networkThread.importantChannelId = config.AddChannel(QosType.AllCostDelivery);
            networkThread.Topology = new HostTopology(config, Settings.maxPlayers);
            text = gameObject.GetComponent<GeneralGUI>().text;
            messages.Add("");
            networkThread.socketID = NetworkTransport.AddHost(networkThread.Topology, Settings.Port);
            thread = new Thread(new ThreadStart(networkThread.StartIt));
            thread.Start();
            ParameterizedThreadStart pst = Settings.SetNetworkAndStartCheckTicks;
            thread2 = new Thread(pst);
            thread2.Start(this);
            gameObject.AddComponent<VoiceChatPlayer>();
            VoiceChatRecorder vcr = gameObject.AddComponent<VoiceChatRecorder>();
            vcr.network = this;
            vcr.NetworkId = networkThread.socketID;
            gameObject.AddComponent<VoiceChat.Scripts.VoiceChatManager>();
            networkThread.StartIt();
        }

        public void Shutdown()
        {
            NetworkTransport.RemoveHost(networkThread.socketID);
            NetworkTransport.Shutdown();
            thread.Abort();
            thread2.Abort();
        }

        public void Update()
        {
            if (show.Pressed())
            {
                showWin = !showWin;
            }
            lock (usersToGameObjects)
            {
                if (usersToGameObjects.Count > 0)
                {
                    lock (networkThread._users)
                    {
                        foreach (int usersToGameObject in usersToGameObjects)
                        {
                            networkThread._users[usersToGameObject].gameObjects = new Dictionary<string, Component>()
                            {
                                {"AddPiece", gameObject.AddComponent<AddPieceMP>()},
                                {"Machine", gameObject.AddComponent<MachineMP>()},
                                {"MOT", gameObject.AddComponent<MachineObjectTracker>()}
                            };
                        }
                    }
                }
            }
            lock (vcps)
            {
                if (vcps.Count > 0)
                {
                    foreach (VoiceChatPacket voiceChatPacket in vcps)
                    {
                        VoiceChatManager.OnClientPacketReceived(voiceChatPacket);
                    }
                }
            }
        }

        #region Legacy
        /*
        private void UserInfoReceivedFromServer(byte[] buffer)
        {
            User us = ConvertUser(buffer);
            byte err;
            Users.Add(us.name, us);
            int connectionID = NetworkTransport.Connect(socketID, us.adress, us.port, 0, out err);
            Users[us.name].gameObjects = new Dictionary<string, Component>
            {
                {"AddPiece", gameObject.AddComponent<AddPiece>()},
                {"Machine", gameObject.AddComponent<Machine>()},
                {"MachineObjectTracker", gameObject.AddComponent<MachineObjectTracker>()}
            };
            us.connectionID = connectionID;
        }

        private void UserInfoReceivedFromClient(String mess, int outconnectID)
        {
            String[] parts = mess.Split(',');
            string name = parts[0].Replace("Name:", "");
            string adress = parts[1].Replace("Adress:", "");
            int port = Int32.Parse(parts[2].Replace("Port:", ""));
            Users.Add(name, new User());
            Users[name].name = name;
            Users[name].connectionID = outconnectID;
            Users[name].adress = adress;
            Users[name].port = port;
            foreach (var value in Users.Values)
            {
                SendUserObject(Users[name], userChannelId, value.connectionID);
            }
            Users[name].gameObjects = new Dictionary<string, Component>
            {
                {"AddPiece", gameObject.AddComponent<AddPiece>()},
                {"Machine", gameObject.AddComponent<Machine>()},
                {"MachineObjectTracker", gameObject.AddComponent<MachineObjectTracker>()}
            };
        }

        private void SendMethod(String method, String methodName, [CanBeNull] String response, int connID)
        {
            responses.Add(methodName, (x) => response);
            byte[] mess = Convert(methodName + ":" + method);
            byte error;
            NetworkTransport.Send(socketID, connID, methodChannelId, mess, mess.Length, out error);
        }

        private void SendChatMessage(string messa)
        {
            foreach (var user in Users.Values)
            {
                SendMessage(Settings.Name + ":" + messa, chatChannelId, user.connectionID);
            }
        }

        private User ConvertUser(byte[] buffer)
        {
            Stream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream) as User;
        }

        private byte[] ConvertObject(object obj)
        {
            byte[] buffer = new byte[1024];
            Stream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return buffer;
        }

        private void SendUserObject(User user, int channelID, int connectionID)
        {
            byte error;
            NetworkTransport.Send(socketID, connectionID, channelID, ConvertObject(user), 1024, out error);
        }

        private void SendMessage(string message, int channelId, int connectionID)
        {
            byte[] buff = Convert(message);
            byte err;
            NetworkTransport.Send(socketID, connectionID, channelId, buff, buff.Length, out err);
        }
        */
        #endregion

        public void OnGUI()
        {
            if (showWin)
            {
                if (!stylesDone)
                {
                    GUI.skin = ModGUI.Skin;
                    windowStyle = new GUIStyle(GUI.skin.window)
                    {
                        normal = new GUIStyleState
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
            lock (messages)
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
            }

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "chatInput" && Regex.IsMatch(message, @"[^\s]"))
            {
                networkThread.ChatmessagesToSend.Add(Settings.Name + ":" + message);
                message = "";
            }

            GUI.SetNextControlName("chatInput");
            message = GUILayout.TextField(message);
            GUI.DragWindow();
        }


        private string Convert(byte[] buffer)
        {
            return Encoding.Unicode.GetString(buffer);
        }

        private byte[] Convert(string buff)
        {
            return Encoding.Unicode.GetBytes(buff);
        }

        public void OnDestroy()
        {
            Shutdown();
        }
    }
}