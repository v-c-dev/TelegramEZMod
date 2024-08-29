using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.Json;
using TelegramEZMod.Services;

namespace TelegramEZMod.Services
{
    public class BotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly Func<long, BlocklistService> _blocklistServiceFactory;
        public TelegramBotClient BotClient => _botClient;

        public BotService(string botToken, Func<long, BlocklistService> blocklistServiceFactory)
        {
            _botClient = new TelegramBotClient(botToken);
            _blocklistServiceFactory = blocklistServiceFactory;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                var blocklistService = _blocklistServiceFactory(chatId);

                if (message.Text.StartsWith("/"))
                {
                    if (await IsUserAdminAsync(chatId, message.From.Id) || message.Text.ToLower() == "/blocklist")
                    {
                        await HandleCommandAsync(message, chatId, blocklistService);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "You do not have permission to perform this action.");
                    }
                }
                else if (blocklistService.IsBlocklistActive())
                {
                    var blockedWords = blocklistService.GetBlocklist();
                    foreach (var word in blockedWords)
                    {
                        if (message.Text.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleBlockedWordAsync(chatId, message, word, blocklistService);
                            break;
                        }
                    }
                }
            }
        }

        private async Task<bool> IsUserAdminAsync(long chatId, long userId)
        {
            var admins = await _botClient.GetChatAdministratorsAsync(chatId);
            return admins.Any(a => a.User.Id == userId);
        }

        private async Task HandleCommandAsync(Message message, long chatId, BlocklistService blocklistService)
        {
            var parts = message.Text.Split(' ', 2);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1] : string.Empty;

            switch (command)
            {
                case "/addblock":
                    blocklistService.AddToBlocklist(argument);
                    await _botClient.SendTextMessageAsync(chatId, $"Added '{argument}' to blocklist.");
                    break;

                case "/rmblock":
                    blocklistService.RemoveFromBlocklist(argument);
                    await _botClient.SendTextMessageAsync(chatId, $"Removed '{argument}' from blocklist.");
                    break;

                case "/clearblock":
                    blocklistService.ClearBlocklist();
                    await _botClient.SendTextMessageAsync(chatId, "Blocklist cleared.");
                    break;

                case "/blocklist":
                    var blocklist = string.Join(", ", blocklistService.GetBlocklist());
                    await _botClient.SendTextMessageAsync(chatId, $"Blocklist: {blocklist}");
                    break;

                case "/blockon":
                    blocklistService.SetBlocklistActive(argument);
                    await _botClient.SendTextMessageAsync(chatId, $"Blocklist is now {(blocklistService.IsBlocklistActive() ? "active" : "inactive")}.");
                    break;

                case "/actblock":
                    blocklistService.SetAction(argument);
                    await _botClient.SendTextMessageAsync(chatId, $"Block action set to '{blocklistService.GetAction()}'.");
                    break;

                case "/delblock":
                    blocklistService.SetDeleteMessage(argument);
                    await _botClient.SendTextMessageAsync(chatId, $"Messages in violation will {(blocklistService.ShouldDeleteMessage() ? "" : "not ")}be deleted.");
                    break;

                case "/bstat":
                    var isActive = blocklistService.IsBlocklistActive();
                    var action = blocklistService.GetAction();
                    var deleteMessage = blocklistService.ShouldDeleteMessage();

                    await _botClient.SendTextMessageAsync(chatId,
                        $"Blocklist Status:\n" +
                        $"Active: {isActive}\n" +
                        $"Action: {action}\n" +
                        $"Delete Messages: {deleteMessage}");
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, "Unknown command.");
                    break;
            }
        }

        private async Task HandleBlockedWordAsync(long chatId, Message message, string word, BlocklistService blocklistService)
        {
            if (blocklistService.ShouldDeleteMessage())
            {
                await _botClient.DeleteMessageAsync(chatId, message.MessageId);
            }

            var blockAction = blocklistService.GetAction();
            switch (blockAction)
            {
                case BlockAction.Warn:
                    await _botClient.SendTextMessageAsync(chatId, $"Warning: Your message contains a blocked word '{word}'.");
                    break;
                case BlockAction.Mute:
                    await _botClient.RestrictChatMemberAsync(chatId, message.From.Id,
                        new ChatPermissions { CanSendMessages = false },
                        untilDate: DateTime.UtcNow.AddMinutes(5));
                    await _botClient.SendTextMessageAsync(chatId, "You have been muted for 5 minutes.");
                    break;
                case BlockAction.Ban:
                    await _botClient.BanChatMemberAsync(chatId, message.From.Id);
                    await _botClient.SendTextMessageAsync(chatId, "You have been banned for violating the blocklist.");
                    break;
            }
        }
    }
}