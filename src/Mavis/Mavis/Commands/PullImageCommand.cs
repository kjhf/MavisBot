﻿using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  record PullImageSubCommand
  {
    public readonly string url;
    public readonly string formattedResponseURL;
    public readonly string jsonProperty;
    public readonly bool isPrivate;
    public readonly SlashCommandBuilder subCommand;

    public PullImageSubCommand(SlashCommandBuilder subCommand, string url, string formattedResponseURL, string jsonProperty, bool isPrivate)
    {
      this.url = url ?? throw new ArgumentNullException(nameof(url));
      this.formattedResponseURL = formattedResponseURL ?? throw new ArgumentNullException(nameof(formattedResponseURL));
      this.jsonProperty = jsonProperty ?? throw new ArgumentNullException(nameof(jsonProperty));
      this.isPrivate = isPrivate;
      this.subCommand = subCommand ?? throw new ArgumentNullException(nameof(subCommand));
    }
  }

  public class PullImageCommand : IMavisMultipleCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, PullImageSubCommand> subCommands = new();

    public string CommandTypeName => "pull-image";
    public IList<string> Names => subCommands.Keys.ToArray();

    public PullImageCommand()
    {
      List<PullImageSubCommand> toAdd = new()
      {
        new PullImageSubCommand(subCommand: new SlashCommandBuilder().WithName("bird").WithDescription("Birb."),
          url: "https://some-random-api.ml/img/birb/",
          formattedResponseURL: "%file%",
          jsonProperty: "link",
          isPrivate: false),

        new PullImageSubCommand(subCommand: new SlashCommandBuilder().WithName("cat").WithDescription("Cat."),
          url: "http://aws.random.cat/meow",
          formattedResponseURL: "%file%",
          jsonProperty: "file",
          isPrivate: false),

        new PullImageSubCommand(subCommand: new SlashCommandBuilder().WithName("dog").WithDescription("Dog."),
          url: "https://random.dog/woof.json",
          formattedResponseURL: "%file%",
          jsonProperty: "url",
          isPrivate: false),

        new PullImageSubCommand(subCommand: new SlashCommandBuilder().WithName("fox").WithDescription("Fox."),
          url: "https://some-random-api.ml/animal/fox",
          formattedResponseURL: "%file%",
          jsonProperty: "image",
          isPrivate: false),

        new PullImageSubCommand(subCommand: new SlashCommandBuilder().WithName("neko").WithDescription("Privately gets a Neko image."),
          url: "https://nekos.life/api/neko",
          formattedResponseURL: "%file%",
          jsonProperty: "neko",
          isPrivate: true),

        new PullImageSubCommand(subCommand: new SlashCommandBuilder().WithName("panda").WithDescription("Panda."),
          url: "https://some-random-api.ml/animal/panda",
          formattedResponseURL: "%file%",
          jsonProperty: "image",
          isPrivate: false)
      };
      subCommands = toAdd.ToDictionary(pair => pair.subCommand.Name, pair => pair);
    }

    public IList<ApplicationCommandProperties> BuildCommands()
    {
      return subCommands.Select(pair => pair.Value.subCommand.Build()).ToArray();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command, string name)
    {
      log.Trace($"Processing {nameof(PullImageCommand)} {name}.");
      var subCommand = subCommands[name];
      JsonElement? json;
      string? message = null;

      try
      {
        json = (JsonElement?)await JSONHelper.GetJsonAsync(subCommand.url).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        json = null;
        message = ("⛔ Couldn't fetch your image: " + ex.Message);
      }

      string? file = null;
      if (json.HasValue)
      {
        var jsonElement = json.Value;
        if (jsonElement.TryGetProperty(subCommand.jsonProperty, out var fileProperty))
        {
          file = subCommand.formattedResponseURL.Replace("%file%", fileProperty.GetString());
        }
        else
        {
          message = ($"⛔ The file property `{subCommand.jsonProperty}` was not found in the response.");
        }
        message = file;
      }

      await command.RespondAsync(embed: (file == null) ? null : new MavisEmbedBuilder(file, messageIsImageUrl: true).BuildFirst(), text: message, ephemeral: subCommand.isPrivate);
    }
  }
}