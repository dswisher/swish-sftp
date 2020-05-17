
namespace Swish.Sftp.Packets
{
    public enum PacketType : byte
    {
        SSH_MSG_DISCONNECT = 1,
        SSH_MSG_UNIMPLEMENTED = 3,
        SSH_MSG_SERVICE_REQUEST = 5,
        SSH_MSG_SERVICE_ACCEPT = 6,
        SSH_MSG_KEXINIT = 20,
        SSH_MSG_NEWKEYS = 21,
        SSH_MSG_KEXDH_INIT = 30,
        SSH_MSG_KEXDH_REPLY = 31,
        SSH_MSG_USERAUTH_REQUEST = 50,
        SSH_MSG_USERAUTH_FAILURE = 51,
        SSH_MSG_USERAUTH_SUCCESS = 52,
        SSH_MSG_USERAUTH_BANNER = 53,
        SSH_MSG_CHANNEL_OPEN = 90
    }
}
