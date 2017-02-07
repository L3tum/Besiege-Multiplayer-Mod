namespace BesiegeMP.Network
{
    enum NetworkMessageEnum : byte
    {
        ChatMessage,
        VoiceChatMessage,
        UserClass,
        InitialUserClassToServer,
        Method,
        MethodResponse,
        Block,
        Ping,
        ServerInfo,
        ServerDistributesEverything,
        UserID,
        UserClassList,
        BlockInfo // Position, Rotation, ID
    }
}
