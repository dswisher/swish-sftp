
namespace Swish.Sftp.Packets
{
    public enum PacketType : byte
    {
        SSH_MSG_UNIMPLEMENTED = 3,
        SSH_MSG_KEXINIT = 20,
        SSH_MSG_KEXDH_INIT = 30,
        SSH_MSG_KEXDH_REPLY = 31
    }
}
