#region usings

using System;
using System.Collections.Generic;
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
        public readonly List<string> Messages = new List<string>();
        private readonly Key _show = Keybindings.AddKeybinding("Chat", new Key(KeyCode.LeftControl, KeyCode.Q));
        public readonly List<int> UsersJoinedNeedingSetup = new List<int>();
        public readonly List<VoiceChatPacket> Vcps = new List<VoiceChatPacket>();
        private string _message = "";
        public NetworkThread NetworkThread;
        private Vector2 _pos;
        private bool _showWin, _stylesDone;
        private Texture2D _text;
        private Thread _thread, _thread2;
        private GUIStyle _windowStyle;
        private Rect _winRect = new Rect(0.0f, Screen.height - 500.0f, 400.0f, 300.0f);

        public void StartIt(bool isServer)
        {
            Util.network = this;
            NetworkThread = new NetworkThread(Settings.ticks, isServer) {Network = this};
            NetworkTransport.Init();
            ConnectionConfig Config = new ConnectionConfig();
            NetworkThread.MiscChannelId = Config.AddChannel(QosType.Reliable);
            NetworkThread.BlockChannelId = Config.AddChannel(QosType.ReliableStateUpdate);
            NetworkThread.ChatChannelId = Config.AddChannel(QosType.Unreliable);
            NetworkThread.MethodChannelId = Config.AddChannel(QosType.Reliable);
            NetworkThread.ImportantChannelId = Config.AddChannel(QosType.AllCostDelivery);
            NetworkThread.Topology = new HostTopology(Config, Settings.maxPlayers);
            _text = gameObject.GetComponent<GeneralGUI>().text;
            Messages.Add("");
            NetworkThread.SocketId = NetworkTransport.AddHost(NetworkThread.Topology, Settings.Port);
            _thread = new Thread(NetworkThread.StartIt);
            _thread.Start();
            ParameterizedThreadStart Pst = Settings.SetNetworkAndStartCheckTicks;
            _thread2 = new Thread(Pst);
            _thread2.Start(this);
            gameObject.AddComponent<VoiceChatPlayer>();
            VoiceChatRecorder Vcr = gameObject.AddComponent<VoiceChatRecorder>();
            Vcr.NetworkId = NetworkThread.SocketId;
            gameObject.AddComponent<VoiceChatManager>();
            NetworkThread.StartIt();
            spaar.Commands.RegisterCommand("Connect", ConnectCallback, "Connects to a Server specified with 'Full.IP.Adress:Port'");
            spaar.Commands.RegisterCommand("Disconnect", DisconnectCallback, "Disconnects you from the Server");
            spaar.Commands.RegisterCommand("GetMyIP", IPCallback, "Returns your IP adress used to connect to your Server");
        }

        private string IPCallback(string[] args, IDictionary<string, string> namedArgs)
        {
            return Settings.adress;
        }

        private string DisconnectCallback(string[] args, IDictionary<string, string> namedArgs)
        {
            byte er;
            NetworkTransport.Disconnect(NetworkThread.SocketId, NetworkThread.ConnectionId, out er);
            return "Disconnected from " + NetworkThread.Server.name;
        }

        private string ConnectCallback(string[] args, IDictionary<string, string> namedArgs)
        {
            byte er;
            String[] adressParts = args[0].Split(':');
            NetworkThread.ConnectionId = NetworkTransport.Connect(NetworkThread.SocketId, adressParts[0], int.Parse(adressParts[1]), 0, out er);
            return "Connected to " + adressParts[0] + "\nConnection ID:" + NetworkThread.ConnectionId;
        }

        public void Shutdown()
        {
            NetworkTransport.RemoveHost(NetworkThread.SocketId);
            NetworkTransport.Shutdown();
            _thread.Abort();
            _thread2.Abort();
        }

        public void Update()
        {
            if (_show.Pressed())
            {
                _showWin = !_showWin;
            }
            lock (UsersJoinedNeedingSetup)
            {
                if (UsersJoinedNeedingSetup.Count > 0)
                {
                    lock (NetworkThread.Users)
                    {
                        for (int Index = 0; Index < UsersJoinedNeedingSetup.Count; Index++)
                        {
                            NetworkThread.Users[UsersJoinedNeedingSetup[Index]].gameObjects = new Dictionary<string, Component>
                            {
                                {"AddPiece", gameObject.AddComponent<AddPieceMP>()},
                                {"Machine", gameObject.AddComponent<MachineMP>()},
                                {"MOT", gameObject.AddComponent<MachineObjectTracker>()}
                            };
                        }
                    }
                }
            }
            lock (Vcps)
            {
                if (Vcps.Count > 0)
                {
                    for (int Index = 0; Index < Vcps.Count; Index++)
                    {
                        VoiceChatManager.OnClientPacketReceived(Vcps[Index]);
                    }
                }
            }
        }

        public void OnGUI()
        {
            if (_showWin)
            {
                if (!_stylesDone)
                {
                    GUI.skin = ModGUI.Skin;
                    _windowStyle = new GUIStyle(GUI.skin.window)
                    {
                        normal = new GUIStyleState
                        {
                            background = _text,
                            textColor = GUI.skin.window.normal.textColor
                        }
                    };
                    _stylesDone = true;
                }
                GUI.skin = ModGUI.Skin;
                _winRect = GUI.Window(_winId, _winRect, WinFunc, "Chat", _windowStyle);
            }
        }

        private void WinFunc(int id)
        {
            lock (Messages)
            {
                if (Messages.Count > 0)
                {
                    _pos = GUILayout.BeginScrollView(_pos);
                    for (int Index = 0; Index < Messages.Count; Index++)
                    {
                        GUILayout.Label(Messages[Index]);
                    }
                    GUILayout.EndScrollView();
                }
            }

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "chatInput" && Regex.IsMatch(_message, @"[^\s]"))
            {
                Util.SendGenericMessageAndFigureOutIfSDE(new NetworkData(NetworkThread.ChatChannelId, new NetworkMessage(Settings.Name + ":" + _message, NetworkMessageEnum.ChatMessage)));
                _message = "";
            }

            GUI.SetNextControlName("chatInput");
            _message = GUILayout.TextField(_message);
            GUI.DragWindow();
        }

        public void OnDestroy()
        {
            Shutdown();
        }

        private delegate string responseDelegate(string message);

        #region Legacy

        /*
        private void UserInfoReceivedFromServer(byte[] buffer)
        {
            User us = ConvertUser(buffer);
            byte err;
            Users.Add(us.name, us);
            int connectionID = NetworkTransport.Connect(SocketId, us.adress, us.port, 0, out err);
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
            NetworkTransport.Send(SocketId, connID, MethodChannelId, mess, mess.Length, out error);
        }

        private void SendChatMessage(string messa)
        {
            foreach (var user in Users.Values)
            {
                SendMessage(Settings.Name + ":" + messa, ChatChannelId, user.connectionID);
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
            NetworkTransport.Send(SocketId, connectionID, channelID, ConvertObject(user), 1024, out error);
        }

        private void SendMessage(string _message, int channelId, int connectionID)
        {
            byte[] buff = Convert(_message);
            byte err;
            NetworkTransport.Send(SocketId, connectionID, channelId, buff, buff.Length, out err);
        }
        */

        #endregion
    }
}