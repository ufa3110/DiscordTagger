п»їusing Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordTagger.Core
{
    public class DiscordTagBot : DiscordTagBotBase
    {
        internal override async Task Subscribe()
        {
            _client.MessageReceived += HandleCommandAsync;
            _client.ReactionAdded += onReact;
            base.Subscribe();
        }

        /// <summary>
        /// РґРµР»Р°РµРј РѕС‚РїРёСЃРєСѓ РѕС‚ СЂРѕР»Рё РїРѕ Р»СЋР±РѕР№ СЂРµР°РєС†РёРё РЅР° РЅР°С€Рµ СЃРѕРѕР±С‰РµРЅРёРµ
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
        /// РЎРѕР±С‹С‚РёСЏ РЅР° РіРѕР»РѕСЃРѕРІС‹С… РєР°РЅР°Р»Р°С… - РІС…РѕРґ\РІС‹С…РѕРґ + РєРѕРјР±РёРЅР°С†РёСЏ
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="LeavedChannel"></param>
        /// <param name="JoinedChannel"></param>
        /// <returns></returns>
        internal override async Task onVoiceChannelUpdatedAsync(SocketUser Sender, SocketVoiceState LeavedChannel, SocketVoiceState JoinedChannel)
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
                    if (!role.Members.Any(_ => _.Id == Sender.Id))
                    {
                        WaitAndAddToGroup(JoinedChannel.VoiceChannel, Sender.Username, 1800000);
                    }
                }
            }
        }


        /// <summary>
        /// РїРѕР»СѓС‡Р°РµС‚ СЃРІСЏР·Р°РЅРЅСѓСЋ СЂРѕР»СЊ РёР· РєСЌС€Р°, РµСЃР»Рё С‚Р°РєР°СЏ РµСЃС‚СЊ
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
        /// РџСЂРѕРІРµСЂРєР° Р°РєС‚РёРІРЅРѕСЃС‚Рё РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ РїРµСЂРµРґ РґРѕР±Р°РІР»РµРЅРёРµРј РІ СЃРІСЏР·Р°РЅРЅСѓСЋ РіСЂСѓРїРїСѓ
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
        /// Р—Р°РґРµСЂР¶РєР° РїРµСЂРµРґ РѕС‚РїСЂР°РІРєРѕР№ СѓРІРµРґРѕРјР»РµРЅРёСЏ - РїСЂРёРіР»Р°С€РµРЅРёСЏ
        /// </summary>
        /// <param name="socketVoiceChannel"></param>
        /// <param name="userName"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private async Task WaitAndCheckUser(SocketVoiceChannel socketVoiceChannel, string userName, int time)
        {
            await Task.Delay(time);
            var implementationItem = _implementationItemsCache.FirstOrDefault(_ => _.ChannelId == socketVoiceChannel.Id);

            if (socketVoiceChannel.Users.Any(_ => _.Username == userName) && implementationItem != null)
            {
                var textChannel = socketVoiceChannel.Guild.TextChannels.FirstOrDefault();
                var message = await textChannel.SendMessageAsync($"user {userName} start plays the <@&{implementationItem.GroupId}> \n" +
                    $"Who would join him? \n" +
                    $"To unsubscribe the notifications add any reaction to this message\n" +
                    $"You will be added to linked group if you played on channel 30+minutes\n"
                    //$"Р§С‚РѕР±С‹ РѕС‚РїРёСЃР°С‚СЊСЃСЏ РѕС‚ СѓРІРµРґРѕРјР»РµРЅРёР№ - РґРѕР±Р°РІСЊ СЂРµР°РєС†РёСЋ РЅР° СЌС‚Рѕ СЃРѕРѕР±С‰РµРЅРёРµ\n" +
                    //$"РћР±С‰Р°СЏСЃСЊ РІ СЃРѕРѕС‚РІРµС‚СЃС‚РІСѓСЋС‰РµРј РіРѕР»РѕСЃРѕРІРѕРј С‡Р°С‚Рµ (30+РјРёРЅСѓС‚) С‚С‹ Р±СѓРґРµС€СЊ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё РґРѕР±Р°РІР»РµРЅ РІ РіСЂСѓРїРїСѓ"
                    );
                _savedMessages.Add(new SavedMessage() { RestUserMessage = message, VoiceChannel = socketVoiceChannel, TextChannel = textChannel });
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
                            var channelName = words[channelindex + 1];
                            var groupName = words[groupindex + 1];

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

        internal async Task SetupChannel(SocketGuild server, string channelName, string groupName, LinkAction action)
        {
            var textChannel = server.TextChannels.FirstOrDefault();
            var role = server.Roles.FirstOrDefault(_ => groupName.ToLower().Contains(_.Name.ToLower()) || _.Name.ToLower().Contains(groupName.ToLower()));
            var channel = server.VoiceChannels.FirstOrDefault(_ => channelName.ToLower().Contains(_.Name.ToLower()) || _.Name.ToLower().Contains(channelName.ToLower()));

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
    }
}
