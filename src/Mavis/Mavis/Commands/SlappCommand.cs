using Discord;
using Discord.WebSocket;
using Mavis.SlappSupport;
using Mavis.Utils;
using NLog;
using SplatTagCore;
using SplatTagDatabase;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class SlappCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static SlappCommandHandler? slappCommandHandler;
    private ulong? myId;

    public string Name => "slapp";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      myId = client.CurrentUser.Id;
      Task.Run(() =>
      {
        string? slappFolder = Environment.GetEnvironmentVariable("SLAPP_DATA_FOLDER");
        if (Directory.Exists(slappFolder))
        {
          log.Info($"Building the SplatTagController from {slappFolder}");
        }
        else
        {
          log.Warn($"Building the SplatTagController from non-existent {slappFolder}");
        }

        try
        {
          var (splatTagController, _) = SplatTagControllerFactory.CreateController(saveFolder: slappFolder);
          slappCommandHandler = new SlappCommandHandler(splatTagController);
          log.Trace("Created SplatTagController");

          // Register a reactions handler.
          client.ReactionAdded += Client_ReactionAdded;
        }
        catch (Exception ex)
        {
          log.Error(ex, "SplatTagController threw an exception: " + ex);
        }
        finally
        {
          client.SetStatusAsync(UserStatus.Online).ConfigureAwait(false);
        }
      });

      var builder = new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Query a Splatoon name on Slapp.")
          .AddOption("query", ApplicationCommandOptionType.String, "What you wanna know. All following parameters are optional. Don't write options in this one!", isRequired: true, isDefault: null)
          ;
      foreach (var (optionType, flagName, description, _) in ConsoleOptions.GetOptionsAsTuple())
      {
        // The only string argument we want here is the query, handled above. Others are not required or not for general usage.
        if (optionType == typeof(string))
          continue;

        if (flagName == "--keepOpen")
          continue;

        var optionBuilder =
          new SlashCommandOptionBuilder()
          .WithName(flagName.ToKebabCase())
          .WithDescription(description)
          .WithDefault(false)
          .WithRequired(false);

        /*
          if (optionType == typeof(string))
          {
            optionBuilder.WithType(ApplicationCommandOptionType.String);
            optionBuilder.AddChoice(flagName, flagName);
            optionBuilder.WithAutocomplete(true);
          }
        */
        if (optionType == typeof(bool))
        {
          optionBuilder.WithType(ApplicationCommandOptionType.Boolean);
        }
        else if (optionType == typeof(int))
        {
          optionBuilder.WithType(ApplicationCommandOptionType.Integer);
          optionBuilder.WithMinValue(1);
          optionBuilder.WithMaxValue(DiscordLimits.MAX_EMBED_RESULTS);
        }
        else
        {
          log.Warn($"Not sure how do handle option {flagName} with type {optionType}.");
          continue;
        }
        builder.AddOption(optionBuilder);
      }

      return builder.Build();
    }

    private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> messageContext, Cacheable<IMessageChannel, ulong> channelContext, SocketReaction reaction)
    {
      if (slappCommandHandler == null)
      {
        log.Error("Slapp ReactionAdded invoked when the Controller has not been created yet.");
      }
      else
      {
        // If the message was written by us, and a non-bot user reacted to it.
        if (reaction.User.IsSpecified && !reaction.User.Value.IsBot)
        {
          IUserMessage? message =
            (messageContext.HasValue ? messageContext.Value : reaction.Message.GetValueOrDefault())
            ?? await messageContext.GetOrDownloadAsync();

          log.Trace($"Found reaction... message={message}, Id={message.Id}, " +
            $"channelContext={channelContext}, HasValue={channelContext.HasValue}, Id={channelContext.Id}, " +
            $" reaction={reaction}, reaction.Emote.Name={reaction.Emote.Name}");

          if (message.Author.Id == this.myId)
          {
            log.Debug($"Reaction message.Id={message.Id} came from us, reaction.Emote.Name={reaction.Emote.Name}, forwarding on...");
            await slappCommandHandler.HandleReaction(message, channelContext, reaction);
          }
          else
          {
            log.Trace($"...but not pursuing as it's not us (failed {message.Author.Id} == {this.myId})");
          }
        }
      }
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      if (slappCommandHandler == null)
      {
        log.Error("Slapp command invoked when the Controller has not been created yet.");
        await command.RespondAsync("⌛ Slapp command handler not initialised yet.");
      }
      else
      {
        await command.DeferAsync(ephemeral: true);
        await slappCommandHandler.Execute(client, command);
      }
    }
  }
}