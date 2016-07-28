#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using spaar.ModLoader;
using spaar.ModLoader.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

#endregion

namespace BesiegeMP
{
    internal class Network : MonoBehaviour
    {
        private readonly int _winId = spaar.ModLoader.Util.GetWindowID();
        private string message = "";
        private readonly List<string> messages = new List<string>();
        private int miscChannelId, userChannelId, chatChannelId, importantChannelId, blockChannelId, methodChannelId, socketID, connectionId, sPort;
        private Vector2 pos;
        private string sAdress;
        private readonly Key show = Keybindings.AddKeybinding("Chat", new Key(KeyCode.LeftControl, KeyCode.Q));
        private bool showWin, stylesDone, isServer;
        private Texture2D text;
        private HostTopology topology;
        private readonly Dictionary<string, User> Users = new Dictionary<string, User>();
        private readonly Dictionary<String, responseDelegate> responses = new Dictionary<string, responseDelegate>();
        private GUIStyle windowStyle;
        private Rect winRect = new Rect(0.0f, Screen.height - 300.0f, 400.0f, 300.0f);

        delegate String responseDelegate(String message);

        public void Start()
        {
            NetworkTransport.Init();
            ConnectionConfig config = new ConnectionConfig();
            miscChannelId = config.AddChannel(QosType.Reliable);
            blockChannelId = config.AddChannel(QosType.ReliableFragmented);
            chatChannelId = config.AddChannel(QosType.Unreliable);
            methodChannelId = config.AddChannel(QosType.Reliable);
            importantChannelId = config.AddChannel(QosType.AllCostDelivery);
            userChannelId = config.AddChannel(QosType.ReliableFragmented);
            topology = new HostTopology(config, Settings.maxPlayers);
            text = gameObject.GetComponent<GeneralGUI>().text;
            messages.Add("");
            socketID = NetworkTransport.AddHost(topology, Settings.ServerPort);
            Game.OnBlockPlaced += GameOnOnBlockPlaced;
        }

        private void GameOnOnBlockPlaced(Transform block)
        {
            int id = block.GetComponent<BlockBehaviour>().GetBlockID();
            Vector3 pos = block.position;
            Quaternion rot = block.rotation;
        }

        public void Shutdown()
        {
            NetworkTransport.RemoveHost(socketID);
            NetworkTransport.Shutdown();
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
                    if (outconnectionId == connectionId)
                    {
                        SendMessage("Name:" + Settings.Name + ",Adress:" + Settings.adress + ",Port:" + Settings.ServerPort, miscChannelId, connectionId);
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
                        break;
                    }

                    if (outchannelId == miscChannelId)
                    {
                        string mess = Convert(buffer);
                        //Client sent his name, adress and port to Server which is now registering him and then sending that info to all Clients
                        if (mess.StartsWith("Name:"))
                        {
                            UserInfoReceivedFromClient(mess, outconnectionId);
                            break;
                        }

                        //Method response received
                        if (mess.StartsWith("MethodResponse"))
                        {
                            String[] parts = mess.Split(':');
                            String response = "";
                            for (int i = 2; i < parts.Length; i++)
                            {
                                response += parts[i];
                            }
                            responses[parts[1]].Invoke(response);
                        }
                        break;
                    }

                    //Client received info of another Client and is now registering him
                    if (outchannelId == userChannelId)
                    {
                        UserInfoReceivedFromServer(buffer);
                        break;
                    }
                    //method received
                    if (outchannelId == methodChannelId)
                    {
                        byte err;
                        String wholemethod = Convert(buffer);
                        String[] parts = wholemethod.Split(':');
                        String method = "";
                        for (int i = 1; i < parts.Length; i++)
                        {
                            method += parts[i];
                        }
                        Func<String> func = () => method;
                        byte[] answer = ConvertObject("MethodResponse:" + parts[0] + ":" + func.Invoke());
                        NetworkTransport.Send(socketID, outconnectionId, miscChannelId, answer, answer.Length, out err);
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


        private string Convert(byte[] buffer)
        {
            return Encoding.Unicode.GetString(buffer);
        }

        private byte[] Convert(string buff)
        {
            return Encoding.Unicode.GetBytes(buff);
        }
    }
}