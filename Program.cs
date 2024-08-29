using Telegram.Bot;
using Telegram.Bot.Polling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TelegramEZMod.Services;
using TelegramLanguageBot;

class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Retrieve BotConfiguration from appsettings.json
                var botConfig = context.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();

                // Register services, delay BlocklistService initialization
                services.AddSingleton<BotService>(provider =>
                {
                    var blocklistServiceFactory = new Func<long, BlocklistService>(chatId =>
                        new BlocklistService(chatId));

                    return new BotService(
                        botConfig.BotToken,
                        blocklistServiceFactory
                    );
                });

                // Register BotHostedService to run the bot
                services.AddHostedService<BotHostedService>();
            })
            .Build();

        host.Run();
    }
}