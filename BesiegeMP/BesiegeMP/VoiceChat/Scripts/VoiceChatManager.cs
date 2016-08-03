using spaar.ModLoader.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using VoiceChat;

namespace BesiegeMP.VoiceChat.Scripts
{
    public class VoiceChatManager : MonoBehaviour
    {
        public delegate void MessageHandler<T>(T data);
        public static event MessageHandler<VoiceChatPacket> VoiceChatPacketReceived;

        private static Network.Network network;

        VoiceChatPlayer player = null;

        void Start()
        {
            network = gameObject.GetComponent<Network.Network>();
            VoiceChatPacketReceived += OnReceivePacket;
            gameObject.AddComponent<AudioSource>();
            player = gameObject.AddComponent<VoiceChatPlayer>();
        }

        void OnDestroy()
        {
            VoiceChatPacketReceived -= OnReceivePacket;
        }

        private void OnReceivePacket(VoiceChatPacket data)
        {
            player.OnNewSample(data);
        }

        #region Network Message Handlers
        internal static void OnClientPacketReceived(VoiceChatPacket packet)
        {
            if (network.networkThread.ServerDistributesEverything && network.networkThread.isServer)
            {
                network.networkThread.voiceChatPacketsToSend.Add(packet);
            }
            VoiceChatPacketReceived?.Invoke(packet);
        }

        #endregion
    }
}
