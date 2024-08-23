using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.Json;
using File = System.IO.File;

namespace TelegramBot.Services
{
    public class BotService
    {
        public readonly TelegramBotClient _botClient;
        private readonly Dictionary<long, List<string>> _blocklist;
        private bool _blocklistActive;
        private string _blockAction;
        private bool _deleteMessage;

        public BotService(string botToken, BlocklistService blocklistService)
        {
            _botClient = new TelegramBotClient(botToken);
            _blocklist = LoadBlocklist();
            _blocklistActive = true;
            _blockAction = "warn"; // Default action
            _deleteMessage = false; // Default: don't delete messages
        }

        private Dictionary<long, List<string>> LoadBlocklist()
        {
            try
            {
                var json = File.ReadAllText("blocklist.json");
                return JsonSerializer.Deserialize<Dictionary<long, List<string>>>(json) ?? new Dictionary<long, List<string>>();
            }
            catch
            {
                return new Dictionary<long, List<string>>();
            }
        }

        private void SaveBlocklist()
        {
            var json = JsonSerializer.Serialize(_blocklist);
            File.WriteAllText("blocklist.json", json);
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                if (message.Text.StartsWith("/"))
                {
                    await HandleCommandAsync(message, chatId);
                }
                else if (_blocklistActive && _blocklist.ContainsKey(chatId))
                {
                    var blockedWords = _blocklist[chatId];
                    foreach (var word in blockedWords)
                    {
                        if (message.Text.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleBlockedWordAsync(chatId, message, word);
                            break;
                        }
                    }
                }
            }
        }

        private async Task HandleCommandAsync(Message message, long chatId)
        {
            var parts = message.Text.Split(' ', 2);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1] : string.Empty;

            switch (command)
            {
                case "/addblock":
                    if (!_blocklist.ContainsKey(chatId))
                        _blocklist[chatId] = new List<string>();
                    _blocklist[chatId].Add(argument);
                    SaveBlocklist();
                    await _botClient.SendTextMessageAsync(chatId, $"Added '{argument}' to blocklist.");
                    break;

                case "/rmblock":
                    if (_blocklist.ContainsKey(chatId) && _blocklist[chatId].Remove(argument))
                    {
                        SaveBlocklist();
                        await _botClient.SendTextMessageAsync(chatId, $"Removed '{argument}' from blocklist.");
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, $"'{argument}' not found in blocklist.");
                    }
                    break;

                case "/clearblock":
                    if (_blocklist.ContainsKey(chatId))
                    {
                        _blocklist[chatId].Clear();
                        SaveBlocklist();
                        await _botClient.SendTextMessageAsync(chatId, "Blocklist cleared.");
                    }
                    break;

                case "/blocklist":
                    if (_blocklist.ContainsKey(chatId))
                    {
                        var blocklist = string.Join(", ", _blocklist[chatId]);
                        await _botClient.SendTextMessageAsync(chatId, $"Blocklist: {blocklist}");
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Blocklist is empty.");
                    }
                    break;

                case "/blockon":
                    _blocklistActive = argument.ToLower() == "y";
                    await _botClient.SendTextMessageAsync(chatId, $"Blocklist is now {(_blocklistActive ? "active" : "inactive")}.");
                    break;

                case "/actblock":
                    _blockAction = argument.ToLower();
                    await _botClient.SendTextMessageAsync(chatId, $"Block action set to '{_blockAction}'.");
                    break;

                case "/delblock":
                    _deleteMessage = argument.ToLower() == "y";
                    await _botClient.SendTextMessageAsync(chatId, $"Messages in violation will {(_deleteMessage ? "" : "not ")}be deleted.");
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, "Unknown command.");
                    break;
            }
        }

        private async Task HandleBlockedWordAsync(long chatId, Message message, string word)
        {
            if (_deleteMessage)
            {
                await _botClient.DeleteMessageAsync(chatId, message.MessageId);
            }

            if (_blockAction == "warn")
            {
                await _botClient.SendTextMessageAsync(chatId, $"Warning: Your message contains a blocked word '{word}'.");
            }
            else if (_blockAction == "mute")
            {
                await _botClient.RestrictChatMemberAsync(chatId, message.From.Id, 
                    new ChatPermissions
                    {
                        CanSendPolls = false,
                        CanSendPhotos = false,
                        CanSendAudios = false,
                        CanSendVideos = false,
                        CanChangeInfo = false,
                        CanPinMessages = false,
                        CanInviteUsers = false,
                        CanSendMessages = false,
                        CanManageTopics = false,
                        CanSendDocuments = false,
                        CanSendVideoNotes = false,
                        CanSendVoiceNotes = false,
                        CanSendOtherMessages = false,
                        CanAddWebPagePreviews = false

                    }, false,DateTime.UtcNow.AddMinutes(1));
                await _botClient.SendTextMessageAsync(chatId, "You have been muted for 5 minutes.");
            }
            else if (_blockAction == "ban")
            {
                await _botClient.BanChatMemberAsync(chatId, message.From.Id);
                await _botClient.SendTextMessageAsync(chatId, "You have been banned for violating the blocklist.");
            }
        }
    }
}