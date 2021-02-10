п»їusing Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordTagger.Core
{
    public class DiscordTagBotBase
    {
        public async Task Run()
        {
            _client = new DiscordSocketClient();

            var token = Environment.GetEnvironmentVariable("DiscordToken");
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            Subscribe();
        }

        internal virtual async Task Subscribe()
        {
            _client.Log += Log;
            _client.UserVoiceStateUpdated += onVoiceChannelUpdated;
            _client.Connected += UpdateCache;
            _client.Connected += DeleteAllOwnMessages;
        }

        internal async Task DeleteAllOwnMessages()
        {
            DeleteAllOwnMessagesAsync();
        }

        internal async Task DeleteAllOwnMessagesAsync()
        {
            var guilds = _client.Guilds;
            foreach (var guild in guilds)
            {
                foreach (var channel in guild.TextChannels)
                {
                    var myMessages = await channel.GetMessagesAsync(100).FirstOrDefaultAsync();
                    myMessages = myMessages.Where(_ => _.Author.Id == _client.CurrentUser.Id).ToList();

                    foreach (var message in myMessages)
                    {
                        await message.DeleteAsync();
                        await Task.Delay(500);
                    }
                }
            }
        }

        /// <summary>
        /// С‡С‚РѕР±С‹ РЅРµ Р¶РґР°С‚СЊ РїРѕС‚РѕРє РІ РѕР±СЂР°Р±РѕС‚С‡РёРєРµ Р°РїРё
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="LeavedChannel"></param>
        /// <param name="JoinedChannel"></param>
        /// <returns></returns>
        internal async Task onVoiceChannelUpdated(SocketUser Sender, SocketVoiceState LeavedChannel, SocketVoiceState JoinedChannel)
        {
            onVoiceChannelUpdatedAsync(Sender, LeavedChannel, JoinedChannel);
        }

        internal virtual async Task onVoiceChannelUpdatedAsync(SocketUser sender, SocketVoiceState leavedChannel, SocketVoiceState joinedChannel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// РїРѕР»СѓС‡Р°РµС‚ СЃРІСЏР·Р°РЅРЅСѓСЋ СЂРѕР»СЊ РёР· РєСЌС€Р°, РµСЃР»Рё С‚Р°РєР°СЏ РµСЃС‚СЊ
        /// </summary>
        /// <param name="socketVoiceChannel"></param>
        /// <returns></returns>
        internal async Task<SocketRole> GetRole(SocketVoiceChannel socketVoiceChannel)
        {
            var roleId = _implementationItemsCache.FirstOrDefault(_ => _?.ChannelId == socketVoiceChannel.Id)?.GroupId;
            if (roleId != null)
            {
                var role = socketVoiceChannel.Guild.Roles.FirstOrDefault(_ => _.Id == roleId);
                return role;
            }
            return null;
        }

        internal async Task UpdateCache()
        {
            UpdateCacheAsync();
        }

        internal async Task UpdateCacheAsync()
        {
            using (var context = new ChannelImplementationContext())
            {
                _implementationItemsCache = context.ImplementationItems.ToList();
            }
        }

        internal Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
            return Task.CompletedTask;
        }

        internal enum LinkAction : int
        {
            None = 0,
            Link = 1,
            Unlink = 2
        }

        internal DiscordSocketClient _client = new DiscordSocketClient();

        internal List<ImplementationItem> _implementationItemsCache { get; set; } = new List<ImplementationItem>();
        internal List<SavedMessage> _savedMessages { get; set; } = new List<SavedMessage>();

    }
}
