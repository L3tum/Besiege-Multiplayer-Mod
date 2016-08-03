using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine.Networking;
using VoiceChat;

namespace BesiegeMP.Network
{
    class NetworkThread
    {
        #region NetworkStuff

        public int miscChannelId, chatChannelId, importantChannelId, blockChannelId, methodChannelId, socketID, connectionId, sPort;
        private string sAdress;
        internal HostTopology Topology;
        internal Network network;

        #endregion

        #region ThreadingStuff

        private int _ticksPerSecond;
        private int _millisecondsPerTick;
        private Timer _timer;

        #endregion 

        private readonly Dictionary<string, ResponseDelegate> _responses = new Dictionary<string, ResponseDelegate>();
        delegate String ResponseDelegate(String message);
        internal readonly Dictionary<int, User> _users = new Dictionary<int, User>();
        private User self;
        internal bool isServer;
        internal bool ServerDistributesEverything;

        internal readonly List<NetworkData> messagesToSendOnce = new List<NetworkData>();
        internal readonly List<NetworkData> messagesToSendToEveryone = new List<NetworkData>();   

        public NetworkThread(int ticks)
        {
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
            self = new User() {adress = Settings.adress, connectionID = -1, gameObjects = null, name = Settings.Name, port = Settings.Port, userSocketID = socketID};
        }

        private void Callback(object state)
        {
            lock (messagesToSendOnce)
            {
                if (messagesToSendOnce.Count > 0)
                {
                    byte er;
                    foreach (NetworkData networkData in messagesToSendOnce)
                    {
                        byte[] buff = NetworkData.ConvertObject(networkData.message);
                        NetworkTransport.Send(socketID, networkData.connectionId, networkData.channelId, buff, buff.Length, out er);
                    }
                    messagesToSendOnce.Clear();
                }
            }
            lock (messagesToSendToEveryone)
            {
                if (messagesToSendToEveryone.Count > 0)
                {
                    byte er;
                    foreach (NetworkData networkData in messagesToSendToEveryone)
                    {
                        byte[] buff = NetworkData.ConvertObject(networkData.message);
                        foreach (User user in _users.Values)
                        {
                            if (user.connectionID != -1)
                            {
                                NetworkTransport.Send(socketID, user.connectionID, networkData.channelId, buff, buff.Length, out er);
                            }
                        }
                    }
                    messagesToSendToEveryone.Clear();
                }
            }
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
                            NetworkMessage message = new NetworkMessage(self, NetworkMessageEnum.InitialUserClassToServer);
                            SendNMessage(new NetworkData(connectionId, miscChannelId, message));
                        }
                        else if (outconnectionId != connectionId)
                        {
                            if (isServer)
                            {
                                NetworkMessage message = new NetworkMessage(Settings.serverDistributesEverything, NetworkMessageEnum.ServerDistributesEverything);
                                SendNMessage(new NetworkData(connectionId, miscChannelId, message));
                            }
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
                    NetworkMessage nMessage = (NetworkMessage)NetworkData.ConvertObject(buffer);
                        if (outchannelId == chatChannelId)
                        {
                            if (nMessage.type == NetworkMessageEnum.ChatMessage)
                            {
                                lock (network.messages)
                                {
                                    network.messages.Add((string)nMessage.data);
                                }
                                if (isServer && Settings.serverDistributesEverything)
                                {
                                    SendNMessageToEveryone(new NetworkData(chatChannelId, nMessage));
                                }
                                break;
                            }
                            if (nMessage.type == NetworkMessageEnum.VoiceChatMessage)
                            {
                                lock (network.vcps)
                                {
                                    network.vcps.Add((VoiceChatPacket) nMessage.data);
                                }
                                if (isServer && Settings.serverDistributesEverything)
                                {
                                    SendNMessageToEveryone(new NetworkData(chatChannelId, nMessage));
                                }
                                break;
                            }
                        }

                        if (outchannelId == miscChannelId)
                        {
                            //Client sent his name, adress and port to Server which is now registering him and then sending that info to all Clients
                            if (isServer && nMessage.type == NetworkMessageEnum.InitialUserClassToServer)
                            {
                                lock (_users)
                                {
                                    _users.Add(outconnectionId, (User) nMessage.data);
                                    _users[outconnectionId].connectionID = outconnectionId;
                                    lock (network.usersToGameObjects)
                                    {
                                        network.usersToGameObjects.Add(outconnectionId);
                                    }
                                    SendNMessageToEveryone(new NetworkData(miscChannelId, new NetworkMessage(_users[outconnectionId], NetworkMessageEnum.UserClass)));
                                }
                                break;
                            }

                            if (!isServer && nMessage.type == NetworkMessageEnum.UserClass)
                            {
                                lock (_users)
                                {
                                    User user = (User)nMessage.data;
                                    if (ServerDistributesEverything)
                                    {
                                        _users.Add(user.connectionID, user);
                                    }
                                    else
                                    {
                                        byte er;
                                        int userID = NetworkTransport.Connect(socketID, user.adress, user.port, 0, out er);
                                        user.connectionID = userID;
                                        _users.Add(userID, user);
                                    }
                                    lock (network.usersToGameObjects)
                                    {
                                        network.usersToGameObjects.Add(user.connectionID);
                                    }
                                }
                            }

                            //Method response received
                            if (nMessage.type == NetworkMessageEnum.MethodResponse)
                            {
                                String message = (string) nMessage.data;
                                String[] parts = message.Split(':');
                                String response = "";
                                for (int i = 2; i < parts.Length; i++)
                                {
                                    response += parts[i];
                                }
                                _responses[parts[1]].Invoke(response);
                                break;
                            }
                            if (!isServer && nMessage.type == NetworkMessageEnum.ServerDistributesEverything)
                            {
                                ServerDistributesEverything = (bool) nMessage.data;
                                break;
                            }
                        }

                        //method received
                        else if (outchannelId == methodChannelId)
                        {
                            byte err;
                            String wholemethod = NetworkData.Convert(buffer);
                            String[] parts = wholemethod.Split(':');
                            String method = "";
                            for (int i = 1; i < parts.Length; i++)
                            {
                                method += parts[i];
                            }
                            Func<String> func = () => method;
                            byte[] answer = NetworkData.ConvertObject("MethodResponse:" + parts[0] + ":" + func.Invoke());
                            NetworkTransport.Send(socketID, outconnectionId, miscChannelId, answer, answer.Length, out err);
                        }

                        //voice chat received
                        else if (outchannelId == voiceChatChannelId)
                        {
                            lock (network.vcps)
                            {
                                NetworkMessage message
                            }
                        }
                        //handle info for blocks etc

                        break;
                    }
            }
        }

        private void SendNMessage(NetworkData data)
        {
            byte error;
            NetworkTransport.Send(socketID, data.connectionId, data.channelId, data.messageBytes, data.messageBytes.Length, out error);
        }

        private void SendNMessageToEveryone(NetworkData data)
        {
            byte error;
            lock (_users)
            {
                foreach (var user in _users.Keys)
                {
                    NetworkTransport.Send(socketID, user, data.channelId, data.messageBytes, data.messageBytes.Length, out error);
                }
            }
        }
    }
}
