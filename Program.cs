using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord.Rest;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DiscordTagger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            var token = Environment.GetEnvironmentVariable("DiscordToken");

            _client.MessageReceived += HandleCommandAsync;
            _client.UserVoiceStateUpdated += onVoiceChannelUpdated;
            _client.Connected += UpdateCache;

            _client.ReactionAdded += onReact;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Connected += DeleteAllOwnMessages;

            // Block this task until the program is closed.
            await Task.Delay(-1);

        }
        /// <summary>
        /// удаляет все сообщения своего авторства
        /// </summary>
        async Task DeleteAllOwnMessages()
        {
            DeleteAllOwnMessagesAsync();
        }

        async Task DeleteAllOwnMessagesAsync()
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
        /// делаем отписку от роли по любой реакции на наше сообщение
        /// </summary>
        /// <param name="message"></param>
        /// <param name="arg2"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task onReact(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel arg2, SocketReaction reaction)
        {
            if (message.Id != 0)
            {
                var savedMessage = _savedMessages.FirstOrDefault(_ => _.RestUserMessage.Id == message.Id);

                if (savedMessage != null)
                {
                    var role = (SocketRole)savedMessage.RestUserMessage.Tags.FirstOrDefault().Value;
                    var user = (SocketGuildUser)reaction.User.Value;
                    await user.RemoveRoleAsync(role);
                }
            }
        }

        /// <summary>
        /// чтобы точно не ждать поток в обработчике апи
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="LeavedChannel"></param>
        /// <param name="JoinedChannel"></param>
        /// <returns></returns>
        private async Task onVoiceChannelUpdated(SocketUser Sender, SocketVoiceState LeavedChannel, SocketVoiceState JoinedChannel)
        {
            onVoiceChannelUpdatedAsync(Sender, LeavedChannel, JoinedChannel);
        }



        /// <summary>
        /// События на голосовых каналах - вход\выход + комбинация
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="LeavedChannel"></param>
        /// <param name="JoinedChannel"></param>
        /// <returns></returns>
        private async Task onVoiceChannelUpdatedAsync(SocketUser Sender, SocketVoiceState LeavedChannel, SocketVoiceState JoinedChannel)
        {
            if (LeavedChannel.VoiceChannel != null)
            {
                Console.WriteLine($"user {Sender?.Username} is leaved channel {LeavedChannel.VoiceChannel?.Name}");
                var channelUsersCount = LeavedChannel.VoiceChannel.Users.Count;
                if (channelUsersCount == 0)
                {
                    var message = _savedMessages.FirstOrDefault(_ => _.VoiceChannel?.Id.Equals(LeavedChannel.VoiceChannel?.Id) ?? false);
                    if (message != null)
                    {
                        message.TextChannel.DeleteMessageAsync(message.RestUserMessage.Id);
                        _savedMessages.Remove(message);
                    }
                }
            }
            if (JoinedChannel.VoiceChannel != null) 
            {
                Console.WriteLine($"user {Sender?.Username} joined channel {JoinedChannel.VoiceChannel?.Name}");

                var channelUsersCount = JoinedChannel.VoiceChannel.Users.Count;
                if (channelUsersCount == 1)
                {
                    WaitAndCheckUser(JoinedChannel.VoiceChannel, JoinedChannel.VoiceChannel.Users.FirstOrDefault().Username, 15000);
                }

                var role = await GetRole(JoinedChannel.VoiceChannel);

                if (role != null)
                {
                    if (!role.Members.Any(_=>_.Id == Sender.Id))
                    {
                        WaitAndAddToGroup(JoinedChannel.VoiceChannel, Sender.Username, 1800000);
                    }
                }
            }
        }

        /// <summary>
        /// получает связанную роль из кэша, если такая есть
        /// </summary>
        /// <param name="socketVoiceChannel"></param>
        /// <returns></returns>
        private async Task<SocketRole> GetRole(SocketVoiceChannel socketVoiceChannel)
        {
            var roleId = _implementationItemsCache.FirstOrDefault(_ => _?.ChannelId == socketVoiceChannel.Id)?.GroupId;

            if (roleId != null)
            {
                var role = socketVoiceChannel.Guild.Roles.FirstOrDefault(_ => _.Id == roleId);
                return role;
            }
            return null;

        }

        /// <summary>
        /// Проверка активности пользователя перед добавлением в связанную группу
        /// </summary>
        /// <param name="socketVoiceChannel"></param>
        /// <param name="userName"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private async Task WaitAndAddToGroup(SocketVoiceChannel socketVoiceChannel, string userName, int time)
        {
            await Task.Delay(time);
            var user = socketVoiceChannel.Users.FirstOrDefault(_ => _.Username == userName);

            if (user != null)
            {
                var role = await GetRole(socketVoiceChannel);
                if (role != null)
                {
                    await user.AddRoleAsync(role);
                }
            }
        }

        /// <summary>
        /// Задержка перед отправкой уведомления - приглашения
        /// </summary>
        /// <param name="socketVoiceChannel"></param>
        /// <param name="userName"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private async Task WaitAndCheckUser(SocketVoiceChannel socketVoiceChannel, string userName, int time)
        {
            await Task.Delay(time);
            var implementationItem = _implementationItemsCache.FirstOrDefault(_ => _.ChannelId == socketVoiceChannel.Id);

            if (socketVoiceChannel.Users.Any(_=>_.Username == userName) && implementationItem != null)
            {
                var textChannel = socketVoiceChannel.Guild.TextChannels.FirstOrDefault();
                var message = await textChannel.SendMessageAsync($"user {userName} start plays the <@&{implementationItem.GroupId}> \n" +
                    $"Who would join him? \n" +
                    $"To unsubscribe the notifications add any reaction to this message\n" +
                    $"You will be added to linked group if you played on channel 30+minutes\n"
                    //$"Чтобы отписаться от уведомлений - добавь реакцию на это сообщение\n" +
                    //$"Общаясь в соответствующем голосовом чате (30+минут) ты будешь автоматически добавлен в группу"
                    );
                _savedMessages.Add(new SavedMessage() { RestUserMessage = message, VoiceChannel = socketVoiceChannel , TextChannel = textChannel});
            }
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg != null)
            {
                var isMeTagged = msg.Tags.Any(_ => _.Type == TagType.UserMention && ((SocketGuildUser)_?.Value)?.Id == _client.CurrentUser.Id);
                var isReplyItSelf = msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot;

                if (isMeTagged && !isReplyItSelf)
                {
                    int pos = 0;
                    if (msg.HasCharPrefix('!', ref pos) || msg.HasMentionPrefix(_client.CurrentUser, ref pos))
                    {
                        var context = new SocketCommandContext(_client, msg);
                        var msgtext = msg.Content.ToLower();

                        if (msgtext.Contains("channel:") && msgtext.Contains("group:"))
                        {
                            var action = LinkAction.None;
                            if (msgtext.Contains("!link"))
                            {
                                action = LinkAction.Link;
                            }
                            else 
                            if (msgtext.Contains("!unlink"))
                            {
                                action = LinkAction.Unlink;
                            }

                            var words = msgtext.Split(' ').ToList();

                            var channelWord = words.FirstOrDefault(_ => _.ToLower().Contains("channel"));
                            var grouplWord = words.FirstOrDefault(_ => _.ToLower().Contains("group"));

                            var channelindex = words.IndexOf(channelWord);
                            var groupindex = words.IndexOf(grouplWord);

                            var server = context.Guild;
                            var channelName = words[channelindex+1];
                            var groupName = words[groupindex+1];

                            await SetupChannel(server, channelName, groupName, action);

                            await UpdateCache();
                        }
                        else
                        if (msgtext.Contains("!help"))
                        {
                            var textChannel = context.Guild.TextChannels.FirstOrDefault();
                            await textChannel?.SendMessageAsync("to set links between channel and group type \" !link channel: {channel name} group: {group name}\"");
                        }
                    }
                }
                
            }
            
        }

        public async Task SetupChannel(SocketGuild server, string channelName, string groupName, LinkAction action)
        {
            var textChannel = server.TextChannels.FirstOrDefault();
            var role = server.Roles.FirstOrDefault(_ => groupName.Contains(_.Name));
            var channel = server.VoiceChannels.FirstOrDefault(_ => channelName.ToLower().Contains(_.Name.ToLower()));

            if (role != null && action != LinkAction.None)
            {
                using (var context = new ChannelImplementationContext())
                {
                    using (var dbContextTransaction = context.Database.BeginTransaction())
                    {
                        context.ImplementationItems.Load();
                        var ExistImplementations = ((IQueryable<ImplementationItem>)context.ImplementationItems).Where(_ => _.ServerId == server.Id && _.ChannelId == channel.Id);
                        var roleImplementation = ExistImplementations?.FirstOrDefault(_ => _.GroupId == role.Id);

                        if (action == LinkAction.Link)
                        {
                            if (roleImplementation == null)
                            {
                                context.ImplementationItems.Add(new ImplementationItem()
                                {
                                    ChannelId = channel.Id,
                                    GroupId = role.Id,
                                    ServerId = server.Id
                                });
                            }
                            else
                            {
                                roleImplementation.ChannelId = channel.Id;
                                roleImplementation.GroupId = role.Id;
                                roleImplementation.ServerId = server.Id;

                                context.ImplementationItems.Update(roleImplementation);
                            }
                        }
                        else
                        {
                            if (roleImplementation != null)
                            {
                                context.ImplementationItems.Remove(roleImplementation);
                            }
                        }

                        context.SaveChanges();
                        dbContextTransaction.Commit();

                    }
                    await textChannel.SendMessageAsync("Acknowlenged!").ConfigureAwait(false);
                }
            }
        }

        private async Task UpdateCache()
        {
            UpdateCacheAsync();
        }

        private async Task UpdateCacheAsync()
        {
            using (var context = new ChannelImplementationContext())
            {
                _implementationItemsCache = context.ImplementationItems.ToList();
            }
        }



        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
            return Task.CompletedTask;
        }

        public enum LinkAction : int
        {
            None = 0,
            Link = 1,
            Unlink  = 2
        }

        private DiscordSocketClient _client = new DiscordSocketClient();

        private List<ImplementationItem> _implementationItemsCache { get; set; } = new List<ImplementationItem>();
        private List<SavedMessage> _savedMessages { get; set; } = new List<SavedMessage>();
    }

    public class SavedMessage
    {
        public RestUserMessage RestUserMessage { get; set; }

        public SocketVoiceChannel VoiceChannel { get; set; }

        public SocketTextChannel TextChannel { get; set; }

    }
}
