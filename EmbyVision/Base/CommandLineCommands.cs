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
                        Console.WriteLine("GetSettings          Retrieves a list of current settings");
                        Console.WriteLine("ForcePreviousServer  Makes sure the only connects to the last used emby connect server");
                        Console.WriteLine("ForcePreviousClient  Makes sure the only connects to the last used client (presuming same server)");
                        Console.WriteLine("StartDelay           Place a delay before the program initilises");
                    } else
                    {
                        switch (Args[0].ToLower())
                        {
                            case "startdelay":
                                Console.WriteLine("StartDelay <Delay in seconds>");
                                Console.WriteLine("Place a delay before the program initilises");
                                break;
                            case "forcepreviousclient":
                                Console.WriteLine("ForcePreviousClient true | false");
                                Console.WriteLine("Makes sure the only connects to the last used client (presuming same server)");
                                break;
                            case "forcepreviousserver":
                                Console.WriteLine("ForcePreviousServer true | false");
                                Console.WriteLine("Makes sure the only connects to the last used emby connect server");
                                break;
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
                case "startdelay":
                    int SDTempInt;
                    if (Args.Length < 1 || !int.TryParse(Args[0], out SDTempInt))
                    {
                        Console.WriteLine("StartDelay <Delay in seconds>");
                        Console.WriteLine("Place a delay before the program initilises");
                        break;
                    }
                    Options.Instance.StartDelay = SDTempInt < 0 ? 0 : (SDTempInt > 60 ? 60 : SDTempInt);
                    Options.Instance.SaveOptions();
                    Console.WriteLine(string.Format("Start delay set to {0} seconds", SDTempInt));
                    break;
                case "forcepreviousclient":
                    bool PCBoolTemp;
                    if (Args.Length < 1 || !bool.TryParse(Args[0], out PCBoolTemp))
                    {
                        Console.WriteLine("ForcePreviousServer true | false");
                        Console.WriteLine("Makes sure the only connects to the last used emby connect server");
                        break;
                    }
                    Options.Instance.ForcePrevClient = PCBoolTemp;
                    Options.Instance.SaveOptions();
                    Console.WriteLine(string.Format("Force Previous Client {0}", PCBoolTemp ? "Enabled" : "Disabled"));
                    break;
                case "forcepreviousserver":
                    bool PSBoolTemp;
                    if (Args.Length < 1 || !bool.TryParse(Args[0], out PSBoolTemp))
                    {
                        Console.WriteLine("ForcePreviousServer true | false");
                        Console.WriteLine("Makes sure the only connects to the last used emby connect server");
                        break;
                    }
                    Options.Instance.ForcePrevServer = PSBoolTemp;
                    Options.Instance.SaveOptions();
                    Console.WriteLine(string.Format("Force Previous Server {0}", PSBoolTemp ? "Enabled" : "Disabled"));
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
                case "getsettings":
                    Console.WriteLine("Current Settings");
                    if (!string.IsNullOrEmpty(Options.Instance.ConnectUsername))
                        Console.WriteLine(string.Format("Connect Username : {0}", Options.Instance.ConnectUsername));
                    if (!string.IsNullOrEmpty(Options.Instance.BasicUsername))
                        Console.WriteLine(string.Format("Basic Username : {0}", Options.Instance.BasicUsername));
                    Console.WriteLine(string.Format("ForcePreviousServer : {0}", Options.Instance.ForcePrevServer.ToString()));
                    Console.WriteLine(string.Format("ForcePreviousClient : {0}", Options.Instance.ForcePrevClient.ToString()));
                    break;
            }
        }
    }
}
