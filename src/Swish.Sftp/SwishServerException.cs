
using System;

namespace Swish.Sftp
{
    public class SwishServerException : Exception
    {
        public SwishServerException(DisconnectReason reason, string message)
            : base(message)
        {
            Reason = reason;
        }


        public DisconnectReason Reason { get; set; }
    }
}
