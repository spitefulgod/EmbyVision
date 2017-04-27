using EmbyVision.Base;
using System;
using static EmbyVision.Rest.RestClient;
using EmbyVision.Speech;
using EmbyVision.Emby;
using System.Threading.Tasks;
using System.Threading;

namespace EmbyVision
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("{0} Version {1}", Options.Instance.Client, Options.Instance.Version);
            Console.WriteLine("Emby media server connection client for the blind and visually impaired");
            Console.WriteLine("");
            // Get some generic objects on.
            Talker Talker = new Talker();
            Listener Listener = new Listener();
            // Handle proxied environments
            System.Net.WebRequest.DefaultWebProxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;


            CommandLineCommands Cmd = new CommandLineCommands();
            CommonSpeechCommands CommonSpeech = new CommonSpeechCommands(Talker, Listener);
            EmbyConnector Emby = new EmbyConnector(Talker, Listener);

            CommonSpeech.Start();
            // Get the external IP address, this is used to check for local machines.
            RestResult Result = Common.GetExternalIPAddr();
            if (Result.Success)
            {
                Options.Instance.ExternalIPAddr = Result.Response;
                Console.WriteLine("Detected external IP address {0}", Result.Response);
            }
            else
            {
                Console.WriteLine("Unable to determine external IP address");
                if (!string.IsNullOrEmpty(Result.Error))
                    Console.WriteLine(Result.Error);
            }

            Talker.Speak("Emby Vision, Speech interface");
            CancellationTokenSource TokenSource = new CancellationTokenSource();
            CancellationToken ConnectCancel = TokenSource.Token;

            // Connect to the server, whatever information we have will be used.
            Task.Run(() =>
            {
                while (Emby != null)
                {
                    if (TokenSource.IsCancellationRequested)
                        break;
                    Emby.Start();
                    // Check if we are connected, if not then try again in 30 seconds.
                    if (Emby != null && !Emby.IsConnected)
                        Thread.Sleep(30000);
                    else
                        break;
                }
            }, ConnectCancel);
            // Go through a loop allowing the user to use the software.
            while (1 == 1 && Emby != null)
            {
                string Command = Console.ReadLine();
                // User input, lets cancel any auto reconnects.
                TokenSource.Cancel();
                // Exit if need be
                if (Command.ToLower() == "exit")
                    break;

                if (Command != null && Command.Trim().Length > 0)
                {
                    if (Command.Substring(0, 1) == ":")
                    {
                        // Process an emulated voice command
                        Listener.EmulateCommand(Command.Substring(1));
                    }
                    else
                    {
                        // Standard Command, send it to the Command class.
                        Cmd.ProcessCommands(Command);
                    }
                }
            }
            Emby.Dispose(); Emby = null;
            CommonSpeech.Dispose(); CommonSpeech = null;
            // Clesn up and exit.
            Cmd = null;
        }
    }
}
