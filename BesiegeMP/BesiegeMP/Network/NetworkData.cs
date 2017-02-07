using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace BesiegeMP.Network
{
    class NetworkData
    {
        public int connectionId = -1;
        public int channelId;
        public NetworkMessage message;
        public byte[] messageBytes;

        public NetworkData(int connid, int channid, NetworkMessage message)
        {
            connectionId = connid;
            channelId = channid;
            this.message = message;
            messageBytes = ConvertObject(message);
        }

        public NetworkData(int channID, NetworkMessage message)
        {
            channelId = channID;
            this.message = message;
            messageBytes = ConvertObject(message);
        }


        internal static string Convert(byte[] buffer)
        {
            return Encoding.Unicode.GetString(buffer);
        }

        internal static byte[] Convert(string buff)
        {
            return Encoding.Unicode.GetBytes(buff);
        }

        internal static User ConvertUser(byte[] buffer)
        {
            Stream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream) as User;
        }

        internal static byte[] ConvertObject(object obj)
        {
            byte[] buffer = new byte[1024];
            Stream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return buffer;
        }

        internal static object ConvertObject(byte[] buffer)
        {
            Stream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }
    }
}
