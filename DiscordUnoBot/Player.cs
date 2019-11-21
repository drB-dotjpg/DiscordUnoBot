using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordUnoBot
{
    public class Player : Helper
    {
        public SocketUser thisUser { get; }
        public string name { get; }
        public List<Card> Cards { get; set; } = new List<Card>();

        public Player(SocketUser user)
        {
            thisUser = user;
            name = user.Username;
        }
    }
}
