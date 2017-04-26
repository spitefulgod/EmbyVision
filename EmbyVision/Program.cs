using EmbyVision.Base;
using System;
using static EmbyVision.Rest.RestClient;
using EmbyVision.Speech;
using EmbyVision.Emby;

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
            using (EmbyConnector Emby = new EmbyConnector(Talker, Listener))
            {
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
                // Connect to the server, whatever information we have will be used.
                Emby.Start();
                // Go through a loop allowing the user to use the software.
                while (1 == 1)
                {
                    string Command = Console.ReadLine();
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
            }
            // Clesn up and exit.
            Cmd = null;
        }
    }
}
