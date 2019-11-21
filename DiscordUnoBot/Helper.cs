using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordUnoBot
{
    public class Helper
    {
        public enum Phase { Pregame, Ingame, PostGame }

        public enum CardColor { Red, Blue, Yellow, Green, All }
        public enum CardType { Number, Skip, DrawTwo, Reverse, Wild, WildDrawFour }

    }
}
