namespace DiscordUnoBot
{
    public class Card : Helper
    {
        public CardColor color { get; }
        public CardType type { get; }
        public int value { get; }

        public Card(CardColor color, CardType type, int value = 0)
        {
            this.color = color;
            this.type = type;
            this.value = value;
        }
    }
}
