using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Base
{
    public class CommandLineCommands
    {
        public void ProcessCommands(string Command)
        {
            if (Command == null)
                return;
            Command = Command.Trim();
            string[] ArgItems = Command.Split();
            string Proc = ArgItems[0].ToLower();
            string[] Args = ArgItems.Skip(1).ToArray();

            switch (Proc)
            {
                case "help": case "?":
                    // Is this a general help or specific?
                    if (Args.Length == 0)
                    {
                        Console.WriteLine("Help                 Gives a list of console commands");
                        Console.WriteLine("Help <Command>       Gives a breakdown of a specific command");
                        Console.WriteLine("EnableEmbyConnect    Enter credentials to connect to the Emby Connect servive");
                        Console.WriteLine("DisableEmbyConnect   Removes any specified Emby Connect credentials");
                        Console.WriteLine("EnableEmbyBasic      Enter credentials for basic LAN emby connection");
                        Console.WriteLine("DisableEmbyBasic     Removes any specified basic connection details");
                        Console.WriteLine("SetDefaultServer     Sets the default server when using basic authentication");
                        Console.WriteLine("GetSettings          Retrieves a list of current settings");
                    } else
                    {
                        switch (Args[0].ToLower())
                        {
                            case "enableembyconnect":
                                Console.WriteLine("EnableEmbyConnect <Username> <Password>");
                                Console.WriteLine("Attempt connection to the emby connect service using the credentials supplied");
                                break;
                            case "disableembyconnect":
                                Console.WriteLine("DisableEmbyConnect");
                                Console.WriteLine("Removes any specified emby connect credentials");
                                break;
                            case "enableembybasic":
                                Console.WriteLine("EnableEmbyBasic <Username> <Password>");
                                Console.WriteLine("Use a default username and password for connecting to local servers should emby connect not be set or available");
                                Console.WriteLine("");
                                Console.WriteLine("EnableEmbyBasic <Username>");
                                Console.WriteLine("Use a default username with a blank password for connecting to local servers should emby connect not be set or available");
                                break;
                            case "disableembybasic":
                                Console.WriteLine("DisableEmbyBasic");
                                Console.WriteLine("Removes any specified basic connection details");
                                break;
                            case "setdefaultserver":
                                Console.WriteLine("SetDefaultServer <HostName> <Port>");
                                Console.WriteLine("Sets the default server when using basic authentication");
                                break;
                            case "getsettings":
                                Console.WriteLine("GetSettings");
                                Console.WriteLine("Retrieves a list of current settings");
                                break;
                            default:
                                Console.WriteLine(string.Format("Unknown Command \"{0}\"", Args[0]));
                                break;
                        }
                    }
                    break;
                case "enableembyconnect":
                    if (Args.Length < 2)
                    {
                        Console.WriteLine("EnableEmbyConnect <Username> <Password>");
                        Console.WriteLine("Attempt connection to the emby connect service using the credentials supplied");
                    }
                    else
                    {
                        Options.Instance.ConnectUsername = Args[0].ToString();
                        Options.Instance.ConnectPassword = Args[1].ToString();
                        Options.Instance.SaveOptions();
                        Console.WriteLine("Emby connect credentials saved");
                    }
                    break;
                case "disableembyconnect":
                    Options.Instance.ConnectUsername = null;
                    Options.Instance.ConnectPassword = null;
                    Options.Instance.SaveOptions();
                    Console.WriteLine("Emby connect credentials removed");
                    break;
                case "enableembybasic":
                    if (Args.Length < 1)
                    {
                        Console.WriteLine("EnableEmbyBasic <Username> <Password>");
                        Console.WriteLine("Use a default username and password for connecting to local servers should emby connect not be set or available");
                        Console.WriteLine("");
                        Console.WriteLine("EnableEmbyBasic <Username>");
                        Console.WriteLine("Use a default username with a blank password for connecting to local servers should emby connect not be set or available");
                    }
                    else
                    {
                        Options.Instance.BasicUsername = Args[0].ToString();
                        Options.Instance.BasicPassword = Args.Length == 1 ? "" : Args[1].ToString();
                        Options.Instance.SaveOptions();
                        Console.WriteLine("Emby basic credentials saved");
                    }
                    break;
                case "disableembybasic":
                    Options.Instance.BasicUsername = null;
                    Options.Instance.BasicPassword = null;
                    Options.Instance.SaveOptions();
                    Console.WriteLine("Emby basic credentials removed");
                    break;
                case "setdefaultserver":
                    if (Args.Length < 2)
                    {
                        Console.WriteLine("SetDefaultServer <HostName> <Port>");
                        Console.WriteLine("Sets the default server when using basic authentication");
                    }
                    else
                    {
                        Options.Instance.DefaultClient = string.Format("http://{0}:{1}", Args[0], Args[1]);
                        Options.Instance.SaveOptions();
                        Console.WriteLine("Default server saved");
                    }
                    break;
                case "getsettings":
                    Console.WriteLine("Current Settings");
                    if (!string.IsNullOrEmpty(Options.Instance.ConnectUsername))
                        Console.WriteLine(string.Format("Connect Username : ", Options.Instance.ConnectUsername));
                    if (!string.IsNullOrEmpty(Options.Instance.BasicUsername))
                        Console.WriteLine(string.Format("Basic Username : ", Options.Instance.BasicUsername));
                    if(!string.IsNullOrEmpty(Options.Instance.DefaultClient))
                        Console.WriteLine(string.Format("Default Client : ", Options.Instance.DefaultClient));
                    break;
            }
        }
    }
}
