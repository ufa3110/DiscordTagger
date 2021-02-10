п»їusing Discord.Rest;
using Discord.WebSocket;

namespace DiscordTagger.Core
{
    public class SavedMessage
    {
        public RestUserMessage RestUserMessage { get; set; }

        public SocketVoiceChannel VoiceChannel { get; set; }

        public SocketTextChannel TextChannel { get; set; }

    }
}
