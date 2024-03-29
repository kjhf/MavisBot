﻿using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mavis.Controllers
{
  public class MavisBotController
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly DiscordSocketClient _client;
    private readonly MavisSlashCommandHandler _slashCommandHandler;
    private ITextChannel? logChannel;

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

    private async Task Client_Ready()
    {
      log.Debug($"...beginning ready handler as {this._client.CurrentUser.Username} ({this._client.CurrentUser.Id}).");
      await this._client.SetStatusAsync(UserStatus.Idle);  // We set online again when Slapp has finished booting.
      string? logChannelStr = Environment.GetEnvironmentVariable("LOGS_CHANNEL");
      if (logChannelStr != null)
      {
        if (ulong.TryParse(logChannelStr, out ulong logChannelULong))
        {
          try
          {
            logChannel = await this._client.GetChannelAsync(logChannelULong) as ITextChannel;
          }
          catch (Exception ex)
          {
            log.Error(ex, "Exception thrown getting the logging channel: ");
            log.Error(ex.ToString());
          }

          if (logChannel == null)
          {
            log.Warn($"X Log channel defined as {logChannelULong} but the ITextChannel was not found. Is the channel accessible?");
          }
          else
          {
            log.Info($"✔ Log channel set to {logChannelStr}");
          }
        }
        else
        {
          log.Error($"Log channel was found in the env variable LOGS_CHANNEL but is not a ulong: {logChannelStr}");
        }
      }
      else
      {
        log.Warn("Log channel was not found in the env variable LOGS_CHANNEL.");
      }

#if DEBUG
      string presence = "--=IN DEV=--";
#else
      string presence = "in the cloud ⛅";
#endif

      if (Debugger.IsAttached)
      {
        presence += " (Debugger Attached)";
      }

      _ = Task.Run(async () =>
      {
        try
        {
          // Set the bot's presence based on the running mode
          await this._client.SetGameAsync(presence).ConfigureAwait(false);

          // Asynchronously initialize the commands
          await InitCommands().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          log.Error(ex, "Ready initialization threw an exception");
          log.Error(ex.ToString());
        }
      });

      log.Debug($"...exiting ready handler as {this._client.CurrentUser.Username} ({this._client.CurrentUser.Id}).");
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
      string message = $"[{Environment.MachineName}] {msg.Source}: {msg.Message}";
      log.Log(level, msg.Exception, message);

      if (level >= LogLevel.Info && this.logChannel != null)
      {
        Task.Run(async () => await logChannel.SendMessagesUnrolled(message));
      }
      return Task.CompletedTask;
    }
  }
}