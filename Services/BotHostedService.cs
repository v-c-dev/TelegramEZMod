using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using TelegramEZMod.Services;

public class BotHostedService : IHostedService
{
    private readonly BotService _botService;
    private readonly TelegramBotClient _botClient;

    public BotHostedService(BotService botService, string botToken)
    {
        _botService = botService;
        _botClient = new TelegramBotClient(botToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await _botService.HandleUpdateAsync(update);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // Handle errors here
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }
}