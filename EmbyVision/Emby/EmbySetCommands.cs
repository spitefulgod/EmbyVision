using EmbyVision.Base;
using EmbyVision.Emby.Classes;
using EmbyVision.Speech;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmbyVision.Emby
{
    /// <summary>
    /// Purely used to set
    /// </summary>
    public class EmbySetCommands
    {
        private EmbyCore Store { get; set; }

        public EmbySetCommands(EmbyCore Store)
        {
            this.Store = Store;
        }
        /// <summary>
        /// Sets default commands and any media content commands from the selected server.
        /// </summary>
        /// <returns></returns>
        public async Task SetCommands(EmbyServer Server)
        {
            this.Store = Store;
            // Defautl context stuff, just to avoid duplications later on the commands
            List<SpeechContextItem> TV = new List<SpeechContextItem>()
            {
                new SpeechContextItem("TV Show", "TV"),
                new SpeechContextItem("TV Shows", "TV"),
                new SpeechContextItem("Series", "TV"),
                new SpeechContextItem("TV Series", "TV"),
                new SpeechContextItem("TV Programs", "TV"),
                new SpeechContextItem("TV Program", "TV"),
                new SpeechContextItem("Box Set", "TV"),
                new SpeechContextItem("Box Sets", "TV")
            };
            List<SpeechContextItem> Movie = new List<SpeechContextItem>()
            {
                new SpeechContextItem("Film", "Movie"),
                new SpeechContextItem("Films", "Movie"),
                new SpeechContextItem("Movie", "Movie"),
                new SpeechContextItem("Movies", "Movie"),
                new SpeechContextItem("Feature", "Movie"),
                new SpeechContextItem("Feature Film", "Movie")
            };
            List<SpeechContextItem> Channels = new List<SpeechContextItem>()
            {
                new SpeechContextItem("TV Channel", "Channel"),
                new SpeechContextItem("Channel", "Channel"),
                new SpeechContextItem("TV Channels", "Channel"),
                new SpeechContextItem("Channels", "Channel"),
                new SpeechContextItem("Program", "Channel")
            };
            List<SpeechContextItem> Servers = new List<SpeechContextItem>()
            {
                new SpeechContextItem("Servers", "Server"),
                new SpeechContextItem("Server", "Server"),
                new SpeechContextItem("Media Center", "Server"),
                new SpeechContextItem("Media Centers", "Server"),
                new SpeechContextItem("Machines", "Server"),
                new SpeechContextItem("Computer", "Server"),
                new SpeechContextItem("Computers", "Server")
            };
            List<string> PlayCommands = new List<string>(new string[] { "Play", "Watch", "Start", "Continue", "Resume" });
            List<string> SkipCommands = new List<string>(new string[] { "Play", "Watch", "Start", "Skip to", "Go to" });
            List<string> Clients = new List<string>(new string[] { "Client", "Clients", "Software Clients", "Software Client" });
            List<string> TimeType = new List<string>(new string[] { "Seconds", "Minutes", "Hours", "Second", "Minute", "Hour" });
            List<SpeechContextItem> All = new List<SpeechContextItem>();
            All.AddRange(Movie);
            All.AddRange(TV);
            All.AddRange(Channels);
            All.AddRange(Servers);

            List<VoiceCommand> Commands = new List<VoiceCommand>();
            // Commands, first the standard, non server restricted stuff, this only needs to be set once, as it works regardless of server.
            if (!Store.Listener.HasCommands("EmbyBase"))
            {
                Commands.Add(new VoiceCommand()
                {
                    Name = "ChangeServer",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Switch", "Change", "Connect"),
                            new OptionalCommandList("to"),
                            new OptionalCommandList("the"),
                            new SelectCommandList("Temp", false, Servers),
                            new SelectCommandList("Server", true, Store.Servers, "ServerName")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "RefreshServerList",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Refresh", "Update"),
                            new OptionalCommandList("the"),
                            new OptionalCommandList("current", "available"),
                            new OptionalCommandList("list of"),
                            new OptionalCommandList("current", "available"),
                            new SelectCommandList("Temp", false, Servers),
                            new OptionalCommandList("list")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "HowMany",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("How many"),
                            new SelectCommandList("Type", false, All),
                            new OptionalCommandList("do I have","are available", "are there", "are connected")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListItems",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read", "Show"),
                                new OptionalCommandList("all"),
                                new OptionalCommandList("available"),
                                new SelectCommandList("Type", false, All),
                                new OptionalCommandList("available"),
                                new OptionalCommandList("on the server")
                                ),
                        new SpeechItem(
                                new CommandList("What"),
                                new SelectCommandList("Type", false, All),
                                new CommandList("are available"),
                                new OptionalCommandList("on the server")
                               )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "CheckAudioTrack",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("How Many", "What"),
                                new CommandList("audio", "sound", "language"),
                                new CommandList("tracks", "streams", "channel", "channels"),
                                new OptionalCommandList("are", "are available", "does this", "are there"),
                                new OptionalCommandList("on this"),
                                new OptionalCommandList(All),
                                new OptionalCommandList("have", "contain")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "SwitchAudioTrack",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Play","Switch","Swap","Listen"),
                                new OptionalCommandList("to"),
                                new CommandList("audio", "sound", "language"),
                                new CommandList("track", "stream", "channel"),
                                new OptionalCommandList("number"),
                                new SelectCommandList("Track", false, Common.NumberList(1, 10)))
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "RefreshMedia",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Refresh", "Update"),
                            new CommandList("Media")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "WhatAmIWatching",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("What am i watching")
                            )
                    }
                });
                await Task.Run(() =>
                {
                    Store.Listener.CreateGrammarList("EmbyBase", Commands);
                });
            }

            // Now the server specific items.
            Commands.Clear();
            if (Store.SelectedServer != null)
            {
                Commands.Add(new VoiceCommand()
                {
                    Name = "PlayItem",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new SelectCommandList("Type", false, Movie),
                                new OptionalCommandList("number"),
                                new SelectCommandList("PlayItem", true, Store.SelectedServer.Movies, "Name")
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new SelectCommandList("Type", false, Channels),
                                new SelectCommandList("PlayItem", true, Store.SelectedServer.TVChannels, "Name")
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new SelectCommandList("Type", false, Channels),
                                new CommandList("number"),
                                new SelectCommandList("PlayItem", true, Store.SelectedServer.TVChannels, "ChannelNumber")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "Pause",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Pause"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(All)
                            ),
                        new SpeechItem(
                                new CommandList("Unpause", "Resume", "Play"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(All)
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "Stop",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Stop"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(All)
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "GotoTVShow",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListTVSeasons",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new OptionalCommandList("all"),
                                new CommandList("seasons")
                            ),
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new OptionalCommandList("all"),
                                new CommandList("seasons"),
                                new OptionalCommandList("for"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "GoToSeason",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList("Season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                            ),
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList("Season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("of"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name")
                            ),
                        new SpeechItem(
                                new CommandList("Go to", "Show", "Select"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name"),
                                new OptionalCommandList("Season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListClients",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read", "Show"),
                                new OptionalCommandList("all"),
                                new OptionalCommandList("available"),
                                new CommandList(Clients),
                                new OptionalCommandList("available", "connected"),
                                new OptionalCommandList("on the server")
                                ),
                        new SpeechItem(
                                new CommandList("What"),
                                new CommandList(Clients),
                                new CommandList("are available", "are connected"),
                                new OptionalCommandList("on the server")
                               )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ChangeClient",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Switch", "Change", "Connect"),
                            new OptionalCommandList("to"),
                            new OptionalCommandList("the"),
                            new CommandList(Clients),
                            new SelectCommandList("Client", true, Store.SelectedServer.Clients, "Client")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "Restart",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Restart", "Start"),
                            new OptionalCommandList("the"),
                            new OptionalCommandList("current"),
                            new CommandList(All),
                            new OptionalCommandList("from the begining")
                        ),
                        new SpeechItem(
                            new CommandList("Go to", "Restart", "Start"),
                            new OptionalCommandList("from"),
                            new OptionalCommandList("the"),
                            new CommandList("begining", "start"),
                            new OptionalCommandList("of", "of the"),
                            new OptionalCommandList("current"),
                            new OptionalCommandList(All)
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ProgramInfo",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Tell me"),
                            new OptionalCommandList("more"),
                            new OptionalCommandList("information", "details"),
                            new CommandList("about what i'm watching")
                        ),
                        new SpeechItem(
                            new CommandList("Tell me"),
                            new OptionalCommandList("more"),
                            new OptionalCommandList("information", "details"),
                            new CommandList("about", "what happens in"),
                            new OptionalCommandList("this", "the current"),
                            new CommandList(All)
                        ),
                        new SpeechItem(
                            new CommandList("What happens", "What is", "Tell me what"),
                            new OptionalCommandList("in"),
                            new OptionalCommandList("this"),
                            new CommandList(All),
                            new OptionalCommandList("is"),
                            new OptionalCommandList("about")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ListTVEpisodes",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new OptionalCommandList("all"),
                                new CommandList("episodes")
                            ),
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new CommandList("all episodes", "episodes"),
                                new OptionalCommandList("for"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                        ),
                        new SpeechItem(
                                new CommandList("List", "Read"),
                                new CommandList("all episodes", "episodes"),
                                new OptionalCommandList("for"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                         )
    ,               }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "PlayTVEpisode",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100))
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100)),
                                new OptionalCommandList("for", "of"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20))
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100))
                            ),
                         new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100)),
                                new OptionalCommandList("for", "of"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new OptionalCommandList("for", "of"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name")
                            ),
                        new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100))
                            ),
                         new SpeechItem(
                                new CommandList(PlayCommands),
                                new OptionalCommandList("viewing", "watching"),
                                new CommandList("season"),
                                new SelectCommandList("Season", false, Common.NumberList(1, 20)),
                                new CommandList("episode"),
                                new SelectCommandList("Episode", false, Common.NumberList(1, 100)),
                                new OptionalCommandList("for", "of"),
                                new OptionalCommandList("the"),
                                new OptionalCommandList(TV),
                                new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name")
                            )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "ContinueTVShow",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList(PlayCommands),
                            new OptionalCommandList("viewing", "watching"),
                            new OptionalCommandList("the"),
                            new CommandList(TV),
                            new SelectCommandList("TVShow",true,Store.SelectedServer.TVShows,"Name")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "NextPrevEpisode",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList(SkipCommands),
                            new OptionalCommandList("the"),
                            new CommandList("previous", "last"),
                            new CommandList("episode")
                        ),
                        new SpeechItem(
                            new CommandList(SkipCommands),
                            new OptionalCommandList("the"),
                            new CommandList("next"),
                            new CommandList("episode")
                        )
                    }
                });
                Commands.Add(new VoiceCommand()
                {
                    Name = "SkipPosition",
                    Commands = new List<SpeechItem>()
                    {
                        new SpeechItem(
                            new CommandList("Skip", "Fast", "Go", "Move"),
                            new SelectCommandList("Direction", false, new string[] {"forward", "backwards" }),
                            new OptionalCommandList("by"),
                            new SelectCommandList("Time1",false,Common.NumberList(1,120)),
                            new SelectCommandList("TimeType1", false, TimeType)
                        ),
                        new SpeechItem(
                            new CommandList("Skip", "Fast", "Go", "Move"),
                            new SelectCommandList("Direction", false, new string[] {"forward", "backwards" }),
                            new OptionalCommandList("by"),
                            new SelectCommandList("Time1",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType1", false, TimeType),
                            new OptionalCommandList("and"),
                            new SelectCommandList("Time2",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType2", false, TimeType)
                        ),
                        new SpeechItem(
                            new CommandList("Skip", "Fast", "Go", "Move"),
                            new SelectCommandList("Direction", false, new string[] {"forward", "backwards" }),
                            new OptionalCommandList("by"),
                            new SelectCommandList("Time1",false,Common.NumberList(1,12)),
                            new SelectCommandList("TimeType1", false, TimeType),
                            new OptionalCommandList("and"),
                            new SelectCommandList("Time2",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType2", false, TimeType),
                            new OptionalCommandList("and"),
                            new SelectCommandList("Time3",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType3", false, TimeType)
                        ),
                        new SpeechItem(
                            new CommandList("Rewind"),
                            new OptionalCommandList("by"),
                            new SelectCommandList("Time1",false,Common.NumberList(1,120)),
                            new SelectCommandList("TimeType1", false, TimeType)
                        ),
                        new SpeechItem(
                            new CommandList("Rewind"),
                            new OptionalCommandList("by"),
                            new SelectCommandList("Time1",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType1", false, TimeType),
                            new OptionalCommandList("and"),
                            new SelectCommandList("Time2",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType2", false, TimeType)
                        ),
                        new SpeechItem(
                            new CommandList("Rewind"),
                            new OptionalCommandList("by"),
                            new SelectCommandList("Time1",false,Common.NumberList(1,12)),
                            new SelectCommandList("TimeType1", false, TimeType),
                            new OptionalCommandList("and"),
                            new SelectCommandList("Time2",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType2", false, TimeType),
                            new OptionalCommandList("and"),
                            new SelectCommandList("Time3",false,Common.NumberList(1,59)),
                            new SelectCommandList("TimeType3", false, TimeType)
                        )
                    }
                });
            }
            await Task.Run(() =>
            {
                Store.Listener.CreateGrammarList("Emby", Commands);
            });
        }
    }
}
