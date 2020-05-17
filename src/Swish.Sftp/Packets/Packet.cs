
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Swish.Sftp.Packets
{
    public abstract class Packet
    {
        public const int MaxPacketSize = 35000;
        public const int PacketHeaderSize = 5;

        public static readonly Dictionary<PacketType, Type> PacketTypes = new Dictionary<PacketType, Type>();


        static Packet()
        {
            var assembly = Assembly.GetAssembly(typeof(Packet));
            var packets = assembly.GetTypes().Where(t => typeof(Packet).IsAssignableFrom(t));
            foreach (var packet in packets)
            {
                try
                {
                    var instance = Activator.CreateInstance(packet) as Packet;
                    PacketTypes[instance.PacketType] = packet;
                }
                catch (Exception)
                {
                }
            }
        }


        public abstract PacketType PacketType { get; }
        public uint PacketSequence { get; set; }


        public byte[] GetBytes()
        {
            using (var writer = new ByteWriter())
            {
                writer.WritePacketType(PacketType);
                InternalGetBytes(writer);
                return writer.ToByteArray();
            }
        }


        public abstract void Load(ByteReader reader);
        protected abstract void InternalGetBytes(ByteWriter writer);
    }
}
