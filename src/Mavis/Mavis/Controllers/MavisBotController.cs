using Discord;
using Discord.WebSocket;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mavis.Controllers
{
  public class MavisBotController
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly DiscordSocketClient _client;
    private readonly MavisSlashCommandHandler _slashCommandHandler;

    public MavisBotController()
    {
#if DEBUG
      const LogSeverity logLevel = LogSeverity.Info;
#else
      const LogSeverity logLevel = LogSeverity.Warning;
#endif // DEBUG

      _client = new DiscordSocketClient(new DiscordSocketConfig
      {
        GatewayIntents =
          GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildEmojis | GatewayIntents.GuildIntegrations |
          GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions
          | GatewayIntents.GuildMessages,  // Will be a privilege in future
        LogLevel = logLevel,
        MessageCacheSize = 0,
      });
      _slashCommandHandler = new MavisSlashCommandHandler(_client);
    }

    public async Task MainLoop(string[] _)
    {
      _client.Log += Log;
      _client.LoggedIn += () => Log(new LogMessage(LogSeverity.Debug, "Client", "Logging in..."));
      _client.Disconnected += (ex) => Log(new LogMessage(LogSeverity.Debug, "Client", $"Disconnected! {ex.Message}"));
      _client.Connected += () => Log(new LogMessage(LogSeverity.Debug, "Client", "Connected!"));
      _client.Ready += Client_Ready;

      await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BOT_TOKEN"));
      await _client.StartAsync();

      // Wait infinitely to stay connected.
      await Task.Delay(Timeout.Infinite);
    }

    private Task Client_Ready()
    {
      log.Debug($"...ready as {this._client.CurrentUser.Username} ({this._client.CurrentUser.Id}).");
      Task.Run(async () => await InitCommands().ConfigureAwait(false));
      return Task.CompletedTask;
    }

    private async Task InitCommands()
    {
      await _slashCommandHandler.InitSlashCommands();
      log.Info($"{nameof(InitCommands)} done.");
    }

    private Task Log(LogMessage msg)
    {
      LogLevel level = msg.Severity switch
      {
        LogSeverity.Critical => LogLevel.Fatal,
        LogSeverity.Debug => LogLevel.Debug,
        LogSeverity.Info => LogLevel.Info,
        LogSeverity.Verbose => LogLevel.Trace,
        LogSeverity.Warning => LogLevel.Warn,
        _ => LogLevel.Error,
      };
      log.Log(level, msg.Exception, msg.Source + ": " + msg.Message);
      return Task.CompletedTask;
    }
  }
}