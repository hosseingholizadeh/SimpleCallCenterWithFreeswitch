using System;
using System.IO;
using System.Threading.Tasks;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.InboundApi;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.OutboundApi;

namespace FreeswitchListenerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                FreeswitchInboundSocketApi.Run();
                FreeswitchOutboundSocketApi.Run();
                var connected = SignalrClient.Start();
                Task ts = ErpContainerDataHelper.ReloadAllData();
                ConsoleMenu.HandleInputs();
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
            SignalrClient.Stop();
        }

        public static void HybridCryptoTest(string privateKeyPath, string privateKeyPassword, string inputPath)
        {
            // Setup the test
            var publicKeyPath = Path.ChangeExtension(privateKeyPath, ".public");
            var outputPath = Path.Combine(Path.ChangeExtension(inputPath, ".enc"));
            var testPath = Path.Combine(Path.ChangeExtension(inputPath, ".test"));

            if (!File.Exists(privateKeyPath))
            {
                var keys = Crypto.GenerateNewKeyPair(2048);
                Crypto.WritePublicKey(publicKeyPath, keys.PublicKey);
                Crypto.WritePrivateKey(privateKeyPath, keys.PrivateKey, privateKeyPassword);
            }

            // Encrypt the file
            var publicKey = Crypto.ReadPublicKey(publicKeyPath);
            Crypto.EncryptFile(inputPath, outputPath, publicKey);
            File.Delete(inputPath);

            // Decrypt it again to compare against the source file
            var privateKey = Crypto.ReadPrivateKey(privateKeyPath, privateKeyPassword);
            Crypto.DecryptFile(outputPath, testPath, privateKey);
            // Check that the two files match
            var dest = File.ReadAllBytes(testPath);
            File.WriteAllBytes(inputPath, dest);
        }
    }
}
