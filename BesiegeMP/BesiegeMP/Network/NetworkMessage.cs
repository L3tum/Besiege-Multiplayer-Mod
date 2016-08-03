namespace BesiegeMP.Network
{
    class NetworkMessage
    {
        internal object data;
        internal NetworkMessageEnum type;

        internal NetworkMessage(object data, NetworkMessageEnum type)
        {
            this.data = data;
            this.type = type;
        }
    }
}
