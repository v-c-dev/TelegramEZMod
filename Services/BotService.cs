using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramEZMod.Services
{
    public class BotService
    {
        private readonly TelegramBotClient _botClient;

        // A dictionary to keep track of warning counts for each user
        private readonly Dictionary<long, int> _warningCounts = new Dictionary<long, int>();

        public BotService(string botToken)
        {
            _botClient = new TelegramBotClient(botToken);
        }

        // This method handles incoming updates (messages) from users
        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;
                var userId = message.From.Id;
                string msg = message.Text;
                int muteTime = 1;
                int ban = 1;

                try
                {
                    
                }
                catch (ApiRequestException apiEx)
                {
                    
                }
            }
        }
    }
}