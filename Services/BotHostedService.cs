using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramEZMod.Services
{
    public class BotHostedService : IHostedService
    {
        private readonly BotService _botService;
        private readonly ReceiverOptions _receiverOptions;

        public BotHostedService(BotService botService)
        {
            _botService = botService;

            // Configure receiver options if needed
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start receiving updates from Telegram
            _botService.BotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                _receiverOptions,
                cancellationToken
            );

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Perform any necessary cleanup when the service is stopped
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Delegate the update handling to BotService
            await _botService.HandleUpdateAsync(update);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Handle errors from the Telegram Bot API
            Console.WriteLine($"Telegram Bot API error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}