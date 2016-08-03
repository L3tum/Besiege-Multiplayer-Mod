using System;
using UnityEngine;
using System.Collections;

namespace VoiceChat
{
    [Serializable]
    public enum VoiceChatCompression : byte
    {
        /*
        Raw, 
        RawZlib, 
        */
        Alaw,
        AlawZlib,
        Speex
    } 
}
