#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine.Networking;
using VoiceChat;

#endregion

namespace BesiegeMP.Network
{
    internal class NetworkThread
    {
        #region NetworkStuff

        public int MiscChannelId, ChatChannelId, ImportantChannelId, BlockChannelId, MethodChannelId, SocketId, ConnectionId, SPort;
        private string _sAdress;
        internal HostTopology Topology;
        internal Network Network;

        #endregion

        #region ThreadingStuff

        private int _ticksPerSecond;
        private int _millisecondsPerTick;
        private Timer _timer;

        #endregion

        private delegate string ResponseDelegate(string message);

        private readonly Dictionary<string, ResponseDelegate> _responses = new Dictionary<string, ResponseDelegate>();
        internal Dictionary<int, User> Users = new Dictionary<int, User>();

        internal readonly List<NetworkData> MessagesToSendOnce = new List<NetworkData>();
        internal readonly List<NetworkData> MessagesToSendToEveryone = new List<NetworkData>();
        internal bool IsServer;
        internal User Server;
        private User _self;
        internal bool ServerDistributesEverything;

        public NetworkThread(int ticks, bool isServer)
        {
            this.IsServer = isServer;
            if (isServer)
            {
                ServerDistributesEverything = Settings.serverDistributesEverything;
            }
            _ticksPerSecond = ticks;
            _millisecondsPerTick = 1000/_ticksPerSecond;
        }

        public void ChangeTicks(int newticks)
        {
            _ticksPerSecond = newticks;
            _millisecondsPerTick = 1000/_ticksPerSecond;
            _timer.Change(0, _millisecondsPerTick);
        }

        public void StartIt()
        {
            _timer = new Timer(Callback, null, 0, _millisecondsPerTick);
            _self = new User {adress = Settings.adress, connectionID = -1, gameObjects = null, name = Settings.Name, port = Settings.Port, userSocketID = SocketId, ID = User.GetNextID()};
        }

        private void Callback(object state)
        {
            #region CheckMessages

            CheckMessagesToSendOnce();
            CheckMessagesToSendAll();

            #endregion

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
                    ConnectEventReceived(outconnectionId);
                    break;
                }
                case NetworkEventType.DisconnectEvent:
                {
                    if (outconnectionId == ConnectionId)
                    {
                        //Revert changes done while playing
                    }
                    break;
                }
                case NetworkEventType.DataEvent:
                {
                    DataEventReceived(outconnectionId, outchannelId, buffer);
                    break;
                }
            }
        }

        #region SendMessage
        private void SendNMessage(NetworkData data)
        {
            byte error;
            NetworkTransport.Send(SocketId, data.connectionId, data.channelId, data.messageBytes, data.messageBytes.Length, out error);
        }

        private void SendNMessageToEveryone(NetworkData data)
        {
            byte error;
            lock (Users)
            {
                foreach (var user in Users.Keys)
                {
                    NetworkTransport.Send(SocketId, user, data.channelId, data.messageBytes, data.messageBytes.Length, out error);
                }
            }
        }
        #endregion

        #region CheckMessages
        private void CheckMessagesToSendOnce()
        {
            lock (MessagesToSendOnce)
            {
                if (MessagesToSendOnce.Count > 0)
                {
                    byte er;
                    foreach (NetworkData networkData in MessagesToSendOnce)
                    {
                        byte[] buff = NetworkData.ConvertObject(networkData.message);
                        NetworkTransport.Send(SocketId, networkData.connectionId, networkData.channelId, buff, buff.Length, out er);
                    }
                    MessagesToSendOnce.Clear();
                }
            }
        }

        private void CheckMessagesToSendAll()
        {
            lock (MessagesToSendToEveryone)
            {
                if (MessagesToSendToEveryone.Count > 0)
                {
                    byte er;
                    foreach (NetworkData networkData in MessagesToSendToEveryone)
                    {
                        byte[] buff = NetworkData.ConvertObject(networkData.message);
                        foreach (User user in Users.Values)
                        {
                            if (user.connectionID != -1)
                            {
                                NetworkTransport.Send(SocketId, user.connectionID, networkData.channelId, buff, buff.Length, out er);
                            }
                        }
                    }
                    MessagesToSendToEveryone.Clear();
                }
            }
        }
        #endregion


        #region ConnectEvent
        private void ConnectEventReceived(int outconnectionId)
        {
            //Connection Accepted. Sending _self to Server
            if (outconnectionId == ConnectionId)
            {
                _self.connectionID = outconnectionId;
                NetworkMessage message = new NetworkMessage(_self, NetworkMessageEnum.InitialUserClassToServer);
                SendNMessage(new NetworkData(ConnectionId, MiscChannelId, message));
            }
        }
        #endregion

        #region DataEvent
        private void DataEventReceived(int outconnectionId, int outchannelId, byte[] buffer)
        {
            NetworkMessage nMessage = (NetworkMessage)NetworkData.ConvertObject(buffer);

            #region ChatChannel
            if (outchannelId == ChatChannelId)
            {
                ChatChannelReceived(nMessage);
            }
            #endregion

            #region MiscChannel
            else if (outchannelId == MiscChannelId)
            {
                MiscChannelReceived(nMessage, outconnectionId);
            }
            #endregion

            #region MethodChannel
            else if (outchannelId == MethodChannelId)
            {
                MethodChannelReceived(nMessage, outconnectionId);
            }
            #endregion 
            //TODO:handle info for blocks etc
        }

        #region ChatChannel

        private void ChatChannelReceived(NetworkMessage nMessage)
        {
            if (nMessage.type == NetworkMessageEnum.ChatMessage)
            {
                ChatMessageReceived(nMessage);
            }
            else if (nMessage.type == NetworkMessageEnum.VoiceChatMessage)
            {
                VoiceChatMessageReceived(nMessage);
            }
        }

        private void ChatMessageReceived(NetworkMessage nMessage)
        {
            lock (Network.Messages)
            {
                Network.Messages.Add((string)nMessage.data);
            }
            if (IsServer && Settings.serverDistributesEverything)
            {
                SendNMessageToEveryone(new NetworkData(ChatChannelId, nMessage));
            }
        }

        private void VoiceChatMessageReceived(NetworkMessage nMessage)
        {
            lock (Network.Vcps)
            {
                Network.Vcps.Add((VoiceChatPacket)nMessage.data);
            }
            if (IsServer && Settings.serverDistributesEverything)
            {
                SendNMessageToEveryone(new NetworkData(ChatChannelId, nMessage));
            }
        }
        #endregion

        #region MiscChannel
        private void MiscChannelReceived(NetworkMessage nMessage, int outconnectionId)
        {
            //Server stuff
            if (IsServer)
            {
                //Client sent his name, adress and port to Server which is now registering him and then sending that info to all Clients
                if (nMessage.type == NetworkMessageEnum.InitialUserClassToServer)
                {
                    InitUClassReceived(nMessage);
                }
            }

            //Non Server Stuff
            else if (!IsServer)
            {
                //Server sent Client info
                if (nMessage.type == NetworkMessageEnum.UserClass)
                {
                    UClassReceived(nMessage);
                }

                //Server sent SDE info
                else if (nMessage.type == NetworkMessageEnum.ServerDistributesEverything)
                {
                    ServerDistributesEverything = (bool)nMessage.data;
                }

                //Server sent info
                else if (nMessage.type == NetworkMessageEnum.ServerInfo)
                {
                    lock (Server)
                    {
                        Server = (User)nMessage.data;
                        Server.connectionID = outconnectionId;
                    }
                }

                //User list received from Server
                else if (nMessage.type == NetworkMessageEnum.UserClassList)
                {
                    lock (Users)
                    {
                        Users = (Dictionary<int, User>) nMessage.data;
                    }
                }

                else if (nMessage.type == NetworkMessageEnum.UserID)
                {
                    _self.ID = (int) nMessage.data;
                }
            }
        }

        //Client sent his name, adress and port to Server which is now registering him and then sending that info to all Clients
        private void InitUClassReceived(NetworkMessage nMessage)
        {
            lock (Users)
            {
                int ID = User.GetNextID();
                Users.Add(ID, (User)nMessage.data);
                Users[ID].ID = ID;
                lock (Network.UsersJoinedNeedingSetup)
                {
                    Network.UsersJoinedNeedingSetup.Add(ID);
                }
                SendNMessageToEveryone(new NetworkData(MiscChannelId, new NetworkMessage(Users[ID], NetworkMessageEnum.UserClass)));
                SendNMessage(new NetworkData(Users[ID].connectionID, MiscChannelId, new NetworkMessage(Users, NetworkMessageEnum.UserClassList)));
                SendNMessage(new NetworkData(Users[ID].connectionID, MiscChannelId, new NetworkMessage(Settings.serverDistributesEverything, NetworkMessageEnum.ServerDistributesEverything)));
                SendNMessage(new NetworkData(Users[ID].connectionID, MiscChannelId, new NetworkMessage(_self, NetworkMessageEnum.ServerInfo)));
                SendNMessage(new NetworkData(Users[ID].connectionID, MiscChannelId, new NetworkMessage(ID, NetworkMessageEnum.UserID)));
            }
        }

        //Server sent Client info
        private void UClassReceived(NetworkMessage nMessage)
        {
            lock (Users)
            {
                User user = (User)nMessage.data;
                if (ServerDistributesEverything)
                {
                    Users.Add(user.ID, user);
                }
                else
                {
                    byte er;
                    int userID = NetworkTransport.Connect(SocketId, user.adress, user.port, 0, out er);
                    user.connectionID = userID;
                    Users.Add(user.ID, user);
                }
                lock (Network.UsersJoinedNeedingSetup)
                {
                    Network.UsersJoinedNeedingSetup.Add(user.ID);
                }
            }
        }
        #endregion

        #region MethodChannel
        private void MethodChannelReceived(NetworkMessage nMessage, int outconnectionId)
        {
            //Method received, executing, sending response
            if (nMessage.type == NetworkMessageEnum.Method)
            {
                MethodReceived(nMessage, outconnectionId);
            }
            //Method sent, has been executed, and now a response has been received
            else if (nMessage.type == NetworkMessageEnum.MethodResponse)
            {
                MethodResponseReceived(nMessage);
            }
        }

        private void MethodReceived(NetworkMessage nMessage, int outconnectionId)
        {
            byte err;
            string wholemethod = (string)nMessage.data;
            string[] parts = wholemethod.Split(':');
            string method = "";
            for (int i = 1; i < parts.Length; i++)
            {
                method += parts[i];
            }
            Func<string> func = () => method;
            byte[] answer = NetworkData.ConvertObject("MethodResponse:" + parts[0] + ":" + func.Invoke());
            NetworkTransport.Send(SocketId, outconnectionId, MethodChannelId, answer, answer.Length, out err);
        }

        private void MethodResponseReceived(NetworkMessage nMessage)
        {
            string message = (string)nMessage.data;
            string[] parts = message.Split(':');
            string response = "";
            for (int i = 2; i < parts.Length; i++)
            {
                response += parts[i];
            }
            _responses[parts[1]].Invoke(response);
        }
        #endregion

        #endregion

    }
}