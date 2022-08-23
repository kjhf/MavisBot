using Discord;
using Discord.Net;
using Discord.WebSocket;
using Mavis.Commands;
using Mavis.Utils;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mavis.Controllers
{
  public class MavisSlashCommandHandler
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly DiscordSocketClient _client;
    private readonly IMavisCommand[] _commands;
    private readonly IMavisMultipleCommand[] _multiCommands;
    private ApplicationCommandProperties[]? _builtCommands;

    public MavisSlashCommandHandler(DiscordSocketClient _client)
    {
      this._client = _client;
      (this._commands, this._multiCommands) = PopulateSlashCommands();
      _client.ApplicationCommandCreated += Client_ApplicationCommandCreated;
      _client.ApplicationCommandDeleted += Client_ApplicationCommandDeleted;
      _client.ApplicationCommandUpdated += Client_ApplicationCommandUpdated;
    }

    private Task Client_ApplicationCommandCreated(SocketApplicationCommand arg)
    {
      log.Debug("Successfully created command " + arg.Name);
      return Task.CompletedTask;
    }

    private Task Client_ApplicationCommandDeleted(SocketApplicationCommand arg)
    {
      log.Debug("Successfully deleted command " + arg.Name);
      return Task.CompletedTask;
    }

    private Task Client_ApplicationCommandUpdated(SocketApplicationCommand arg)
    {
      log.Debug("Successfully updated command " + arg.Name);
      return Task.CompletedTask;
    }

    private static (IMavisCommand[], IMavisMultipleCommand[]) PopulateSlashCommands()
    {
      Dictionary<string, IMavisCommand> commands = new();
      foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IMavisCommand))))
      {
        var fromConstructor = type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
        if (fromConstructor is IMavisCommand toAdd)
        {
          bool isNew = commands.TryAdd(toAdd.Name, toAdd);
          if (isNew)
          {
            log.Trace($"Added {nameof(IMavisCommand)} instance of {toAdd.Name} of type {type}.");
          }
          else
          {
            log.Error($"Could not add a second instance of {toAdd.Name} of type {type}.");
          }
        }
        else
        {
          log.Error($"Couldn't add the {nameof(IMavisCommand)} of type {type} as it doesn't have an empty constructor.");
        }
      }

      Dictionary<string, IMavisMultipleCommand> multiCommands = new();
      foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(mytype => mytype.GetInterfaces().Contains(typeof(IMavisMultipleCommand))))
      {
        object? fromConstructor;
        try
        {
          fromConstructor = type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
        }
        catch (Exception ex)
        {
          log.Error(ex, $"Couldn't add the {nameof(IMavisMultipleCommand)} of type {type} because the constructor threw an exception: {ex.Message}");
          log.Info(ex, ex.ToString());
          continue;
        }

        if (fromConstructor is IMavisMultipleCommand toAdd)
        {
          bool isNew = multiCommands.TryAdd(toAdd.CommandTypeName, toAdd);
          if (isNew)
          {
            log.Trace($"Added {nameof(IMavisMultipleCommand)} instance of {toAdd.CommandTypeName} of type {type}.");
          }
          else
          {
            log.Error($"Could not add a second instance of {toAdd.CommandTypeName} of type {type}.");
          }
        }
        else
        {
          log.Error($"Couldn't add the {nameof(IMavisMultipleCommand)} of type {type} as it doesn't have an empty constructor.");
        }
      }

      return (commands.Values.ToArray(), multiCommands.Values.ToArray());
    }

    private async Task Client_SlashCommandExecuted(SocketSlashCommand command)
    {
      log.Trace($"Processing SlashCommandExecuted {command.Data.Name} in channel {command.Channel} sent by {command.User}.");
      if (command.Data.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
      {
        await command.RespondAsync(
          $"Hi! I'm Mavis! I'm a C# bot written by Slate connected to {this._client.Guilds.Count} servers " +
          $"and I know {_commands.Length + _multiCommands.Sum(c => c.Names.Count)} commands! " +
          $"Have a look down the / menu to see what I can do!").ConfigureAwait(false);
      }
      else
      {
        IMavisCommand? mavisCommand = Array.Find(_commands, c => c.Name.Equals(command.Data.Name));
        try
        {
          if (mavisCommand is not null)
          {
            await mavisCommand.Execute(_client, command).ConfigureAwait(false);
          }
          else
          {
            IMavisMultipleCommand? mavisMultiCommand = Array.Find(_multiCommands, c => c.Names.Contains(command.Data.Name));
            if (mavisMultiCommand is not null)
            {
              await mavisMultiCommand.Execute(_client, command, command.Data.Name).ConfigureAwait(false);
            }
            else
            {
              log.Error($"Don't have a command for the registered slash command {command.CommandName}. Check public {nameof(IMavisCommand)} or {nameof(IMavisMultipleCommand)} implementation.");
            }
          }
        }
        catch (Exception ex)
        {
          log.Error(ex, $"The slash command {command.CommandName} threw an unhandled exception: {ex.Message}");
          log.Info(ex, ex.ToString());
        }
      }
    }

    internal async Task InitSlashCommands()
    {
      _client.SlashCommandExecuted += Client_SlashCommandExecuted;

      if (this._client.Guilds.Count == 0)
      {
        log.Error($"No guilds have been loaded to initialise the slash commands. Check OnReady. (The invite link is: {Constants.BotInviteLink} )");
      }

      if (this._commands.Length == 0)
      {
        log.Error($"No {nameof(IMavisCommand)}s have been loaded to initialise the slash commands.");
      }

      try
      {
        _builtCommands = FinaliseCommands();
      }
      catch (Exception exception)
      {
        log.Fatal(exception, $"Failed to create/finalise the slash commands: {exception.Message}");
        log.Info(exception);
        return;
      }

      /*
      foreach (var guild in this._client.Guilds)
      {
        try
        {
          await guild.DeleteApplicationCommandsAsync().ConfigureAwait(false);
          // await guild.BulkOverwriteApplicationCommandAsync(_builtCommands).ConfigureAwait(false);
        }
        catch (HttpException exception)
        {
          log.Error(exception, $"Http Exception with server {guild.Id} in creating the slash commands: {exception.Message} ({exception.DiscordCode})");
          log.Info(exception);
          log.Info(JsonConvert.SerializeObject(exception.Errors, Formatting.Indented));
        }
        catch (Exception exception)
        {
          log.Error(exception, $"Failed to create the slash commands for server {guild.Id}: {exception.Message}");
          log.Info(exception);
        }
      }
      */
      //await _client.BulkOverwriteGlobalApplicationCommandsAsync(_builtCommands).ConfigureAwait(false);
      _client.JoinedGuild += Client_JoinedGuild;
    }

    private async Task Client_JoinedGuild(SocketGuild guild)
    {
      try
      {
        Debug.Assert(_builtCommands != null, "Client_JoinedGuild handler fired before FinaliseCommands finished.");
        await guild.BulkOverwriteApplicationCommandAsync(_builtCommands).ConfigureAwait(false);
      }
      catch (Exception exception)
      {
        log.Error(exception, $"Failed to create the slash commands for server {guild.Id}: {exception.Message}");
        log.Info(exception);
      }
    }

    private ApplicationCommandProperties[] FinaliseCommands()
    {
      return
        this._commands.Select(c =>
        {
          log.Trace($"Building {c.Name} Command");
          return c.BuildCommand(client: _client);
        })
        .Concat(this._multiCommands.SelectMany(c => c.BuildCommands()))
        .ToArray();
    }
  }
}