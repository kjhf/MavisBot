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

    public string Name => "slapp";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
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
      });

      var builder = new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Query a Splatoon name on Slapp.")
          .AddOption("query", ApplicationCommandOptionType.String, "What you wanna know. All following parameters are optional. Don't write options in this one!", isRequired: true)
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
        if ((messageContext.HasValue && !messageContext.Value.Author.IsBot) || (reaction.User.IsSpecified && !reaction.User.Value.IsBot))
        {
          await slappCommandHandler.HandleReaction(messageContext, channelContext, reaction);
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
        await slappCommandHandler.Execute(client, command);
      }
    }
  }
}