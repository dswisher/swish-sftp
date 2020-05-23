
using System;
using System.Collections.Generic;
using System.Linq;

using Swish.Sftp.Ciphers;
using Swish.Sftp.Compressions;
using Swish.Sftp.HostKeyAlgorithms;
using Swish.Sftp.KexAlgorithms;
using Swish.Sftp.MACAlgorithms;


namespace Swish.Sftp
{
    public static class KeyInfo
    {
        public static readonly List<Algo> SupportedCiphers = new List<Algo>();
        public static readonly List<Algo> SupportedKeyExchanges = new List<Algo>();
        public static readonly List<Algo> SupportedHostKeyAlgorithms = new List<Algo>();
        public static readonly List<Algo> SupportedMACAlgorithms = new List<Algo>();
        public static readonly List<Algo> SupportedCompressions = new List<Algo>();


        static KeyInfo()
        {
            AddAlgo<TripleDESCBC>(SupportedCiphers);

            AddAlgo<DiffieHellmanGroup14SHA1>(SupportedKeyExchanges);

            AddAlgo<SSHRSA>(SupportedHostKeyAlgorithms);

            AddAlgo<HMACSHA1>(SupportedMACAlgorithms);

            AddAlgo<NoCompression>(SupportedCompressions);
        }


        public static IKexAlgorithm PickKexAlgorithm(IEnumerable<string> candidateNames)
        {
            foreach (var algoName in candidateNames)
            {
                var match = SupportedKeyExchanges.FirstOrDefault(x => x.Name.Equals(algoName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    var algo = Activator.CreateInstance(match.Type) as IAlgorithm;

                    return (IKexAlgorithm)algo;
                }
            }

            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, "Could not find a shared Kex Algorithm");
        }


        public static IHostKeyAlgorithm PickHostKeyAlgorithm(IEnumerable<string> candidateNames)
        {
            foreach (var algoName in candidateNames)
            {
                var match = SupportedHostKeyAlgorithms.FirstOrDefault(x => x.Name.Equals(algoName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    var algo = Activator.CreateInstance(match.Type) as IAlgorithm;

                    return (IHostKeyAlgorithm)algo;
                }
            }

            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, "Could not find a shared Host Key Algorithm");
        }


        public static ICipher PickCipher(IEnumerable<string> candidateNames, string direction)
        {
            foreach (var algoName in candidateNames)
            {
                var match = SupportedCiphers.FirstOrDefault(x => x.Name.Equals(algoName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    var algo = Activator.CreateInstance(match.Type) as IAlgorithm;

                    return (ICipher)algo;
                }
            }

            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, $"Could not find a shared {direction} Cipher Algorithm");
        }


        public static IMACAlgorithm PickMACAlgorithm(IEnumerable<string> candidateNames, string direction)
        {
            foreach (var algoName in candidateNames)
            {
                var match = SupportedMACAlgorithms.FirstOrDefault(x => x.Name.Equals(algoName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    var algo = Activator.CreateInstance(match.Type) as IAlgorithm;

                    return (IMACAlgorithm)algo;
                }
            }

            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, $"Could not find a shared {direction} MAC Algorithm");
        }


        public static ICompression PickCompression(IEnumerable<string> candidateNames, string direction)
        {
            foreach (var algoName in candidateNames)
            {
                var match = SupportedCompressions.FirstOrDefault(x => x.Name.Equals(algoName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    var algo = Activator.CreateInstance(match.Type) as IAlgorithm;

                    return (ICompression)algo;
                }
            }

            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, $"Could not find a shared {direction} MAC Algorithm");
        }


        private static void AddAlgo<T>(List<Algo> list)
            where T : IAlgorithm
        {
            // Create a temporary instance, so we can grab the name.
            var temp = Activator.CreateInstance<T>();

            list.Add(new Algo(temp.Name, typeof(T)));
        }


        public class Algo
        {
            public Algo(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; private set; }
            public Type Type { get; private set; }
        }
    }
}
