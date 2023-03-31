using Discord;
using Discord.Addons.Utils;
using Discord.Interactions;
using Discord.WebSocket;
using HentaiChanBot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Discord.Addons.Data;
using Discord.Addons.SetupModule;
using HentaiChanBot.API.Rule34;
using HentaiChanBot.Modules;

namespace HentaiChanBot {
    internal class Bot {
        private const string CONFIGURATION_PATH = "CONFIGURATION";

        private static readonly DiscordSocketClient _client = new();
        private static readonly IServiceProvider _provider = CreateServices();

        public static Task Main(string[] args) => MainAsync();

        private static async Task MainAsync() {
            _provider.ActivateServices();
            var logger = _provider.GetService<ILogger<Bot>>();
            var configProvider = _provider.GetRequiredService<IDiscordConfigurationHelper>();
            var token = (await configProvider.GetBotObject<BotConfig>())?.Token;
            logger?.LogInformation("Fetched token: " + token);
            try {
                logger?.LogInformation("Validating token...");
                TokenUtils.ValidateToken(TokenType.Bot, token);
            } catch {
                await configProvider.AllocateObject<BotConfig>(location: ObjectLocation.Bot);
                logger?.LogError("Validation fail! Press any key to continue...");
                Console.ReadKey();
                return;
            }
            logger?.LogInformation("Validation success!");
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            logger?.LogInformation("Bot started");
            await Task.Delay(-1);
        }

        private static IServiceProvider CreateServices() {
            var services = new ServiceCollection();
            //logging
            services.AddLogging(x => x
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole());
            //required
            services
                .AddSingleton(_client)
                .AddSingleton<InteractionService>()
                .AddConfigurationServices(CONFIGURATION_PATH);
            //other
            services.AddActivator(x => x
                .AddCleaner()
                .AddModules(y => y
                    .Add<HentaiCommandModule>()
                    .Add<SmashSlashPassCommandModule>()
                    .AddConfigSetupModule(z => z
                        .Add<SmashSlashPassConfig>())));
            services.AddSingleton<Rule34Api>();
            return services.BuildServiceProvider();
        }
    }
}