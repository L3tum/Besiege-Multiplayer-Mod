using BesiegeMP.Network;
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

        VoiceChatPlayer player = null;

        void Start()
        {
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
            Util.SendGenericMessageAndFigureOutIfSDE(new NetworkData(Util.network.NetworkThread.ChatChannelId, new Network.NetworkMessage(packet, NetworkMessageEnum.VoiceChatMessage)));
            VoiceChatPacketReceived?.Invoke(packet);
        }

        #endregion
    }
}
