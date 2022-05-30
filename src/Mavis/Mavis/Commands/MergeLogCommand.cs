using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using SplatTagDatabase;
using SplatTagDatabase.Merging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  internal class MergeLogCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "merge-log";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Slapp Merge Log actions.")
          .AddOption(new SlashCommandOptionBuilder()
            .WithName("id").WithType(ApplicationCommandOptionType.String)
            .WithDescription("A Slapp Id that you want to check the merge log for.").WithRequired(true)
            )
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      string? input = command.Data.Options.FirstOrDefault()?.Value?.ToString();
      log.Trace($"Processing {Name} with arg {input}.");

      if (string.IsNullOrWhiteSpace(input))
      {
        const string err = "❌ Nothing specified?";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      if (!Guid.TryParse(input, out Guid searchId))
      {
        const string err = "❌ Err... that's not an id?";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      await command.DeferAsync(ephemeral: true);

      List<MergeLogEntry> records;
      try
      {
        records = MergeLogReader.Read();
      }
      catch (Exception ex)
      {
        const string err = "❌ Error reading merge log. Blame Slate 😓";
        await command.ModifyOriginalResponseAsync((message) => message.Content = err).ConfigureAwait(false);
        log.Error(ex, "Error reading merge log: " + ex.Message);
        return;
      }

      var filteredRecords = records.Where(r => r.MergedItemId == searchId || r.FinalItemId == searchId).Select(r => r.ToString()).ToList();
      MavisEmbedBuilder builder = new MavisEmbedBuilder()
        .WithTitle($"Merge Log for {searchId}")
        .WithDescription($"{filteredRecords.Count} entries found.")
        .WithRandomColor();
      builder.AddUnrolledList("Entries", filteredRecords);

      foreach (var embed in builder.SmartBuild())
      {
        // Send the responses
        await command.ModifyOriginalResponseAsync((message) => message.Embed = embed).ConfigureAwait(false);
      }
    }
  }
}