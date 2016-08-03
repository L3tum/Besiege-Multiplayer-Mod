using System;
using UnityEngine;
using System.Collections;

namespace VoiceChat
{
    [Serializable]
    public struct VoiceChatPacket
    {
        public VoiceChatCompression Compression;
        public int Length;
        public byte[] Data;
        public ulong PacketId;
    }

}