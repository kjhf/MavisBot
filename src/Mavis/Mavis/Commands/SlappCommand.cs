using Discord;
using Discord.WebSocket;
using Mavis.SlappSupport;
using Mavis.Utils;
using NLog;
using SplatTagCore;
using SplatTagDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class SlappCommand : IMavisCommand
  {
    private static readonly IReadOnlyList<string> numbersKeyCaps = new List<string>
    {
      "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟",
      // Thanks to GabrielDoddOfficialYT -- https://emoji.gg/user/436716390246776833
      "<:keycap_11:895381504023199825>",
      "<:keycap_12:895381503977087026>",
      "<:keycap_13:895381504048386078>",
      "<:keycap_14:895381504467824691>",
      "<:keycap_15:895381504560095322>",
      "<:keycap_16:895381504505557032>",
      "<:keycap_17:895381504576864296>",
      "<:keycap_18:895381504983719937>",
      "<:keycap_19:895381504476196864>",
      "<:keycap_20:895381504149041225>"
    };

    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static SplatTagController? splatTagController = null;

    public string Name => "slapp";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      Task.Run(() =>
      {
        log.Trace("Making SplatTagController");
        (SlappCommand.splatTagController, _) = SplatTagControllerFactory.CreateController();
        log.Trace("Created SplatTagController");
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

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      if (splatTagController == null)
      {
        await command.RespondAsync(text: "⌛ Slapp not initialised yet.", ephemeral: true).ConfigureAwait(false);
        return;
      }

      Dictionary<string, object> commandParams = command.Data.Options.ToDictionary(kv => kv.Name, kv => kv.Value);
      log.Trace($"Processing {Name} with params: {string.Join(", ", commandParams.Select(kv => kv.Key + "=" + kv.Value))} ");

      SplatTagController.Verbose = commandParams.GetWithConversion("--verbose", false);
      string query = commandParams.GetWithConversion("query", "");

      var options = new MatchOptions
      {
        IgnoreCase = !(commandParams.GetWithConversion("--exact-case", false)),
        NearCharacterRecognition = !(commandParams.GetWithConversion("--exact-character-recognition", false)),
        QueryIsRegex = commandParams.GetWithConversion("--query-is-regex", false),
        Limit = commandParams.GetWithConversion("--limit", 20)
      };

      // If this is a friend code query
      if (query.StartsWith("sw-", StringComparison.OrdinalIgnoreCase))
      {
        string param = query[3..];
        try
        {
          if (FriendCode.TryParse(param, out var fc))
          {
            options.FilterOptions = FilterOptions.FriendCode;
            options.IgnoreCase = false;
            query = fc.ToString();
          }
        }
        catch (Exception e)
        {
          log.Debug($"Query started with SW- but was not a friend code: {e} ");
        }
      }
      log.Trace($"Building result for {query}.");

      await command.RespondAsync(text: "<a:typing:897094396502224916> Just a sec!", ephemeral: true);
      _ = Task.Run(async () =>
      {
        var (embed, colour, reacts) = await ProcessSlappCommand(query, options);
        await command.FollowupAsync(
          text: embed == null ? "No embed was made. 😔" : null,
          embed: embed?.WithColor(colour).Build(),
          ephemeral: false)
          .ConfigureAwait(false);

        try
        {
          await command.DeleteOriginalResponseAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          log.Debug($"Exception deleting the original response to {command.Data.Name}. {ex.Message}");
          log.Trace(ex);
        }

        log.Trace($"Finished {query} processing");
      }).ConfigureAwait(false);
    }

    private static async Task<(EmbedBuilder? builder, Color embedColour, Dictionary<string, IReadonlySourceable> reacts)>
      ProcessSlappCommand(string query, MatchOptions options)
    {
      if (splatTagController == null)
      {
        return (null, Color.Default, new Dictionary<string, IReadonlySourceable>());
      }

      var players = splatTagController.MatchPlayer(query, options);
      var teams = splatTagController.MatchTeam(query, options);
      var additionalTeams = new Dictionary<Guid, Team>();
      var playersForTeams = new Dictionary<Guid, (Player, bool)[]>();
      var placementsForPlayers = new Dictionary<Guid, Dictionary<string, Bracket[]>>();

      bool hasPlayers = players.Length > 0;
      bool hasPlayersPl = players.Length > 1;
      bool hasTeams = teams.Length > 0;
      bool hasTeamsPl = teams.Length > 1;

      if (hasPlayers || hasTeams)
      {
        additionalTeams =
          players
          .SelectMany(p => p.TeamInformation.GetTeamsUnordered().Select(id => splatTagController.GetTeamById(id)))
          .Distinct()
          .ToDictionary(t => t.Id, t => t);
        additionalTeams[Team.NoTeam.Id] = Team.NoTeam;
        additionalTeams[Team.UnlinkedTeam.Id] = Team.UnlinkedTeam;

        playersForTeams =
          teams
          .ToDictionary(t => t.Id, t => splatTagController.GetPlayersForTeam(t));

        foreach (var pair in playersForTeams)
        {
          foreach ((Player, bool) tuple in pair.Value)
          {
            foreach (Guid t in tuple.Item1.TeamInformation.GetTeamsUnordered())
            {
              additionalTeams.TryAdd(t, splatTagController.GetTeamById(t));
            }
          }
        }

        var sources = new HashSet<string>();
        foreach (var s in players.SelectMany(p => p.Sources))
        {
          sources.Add(s.Name);
        }
        foreach (var s in teams.SelectMany(t => t.Sources))
        {
          sources.Add(s.Name);
        }
        foreach (var s in additionalTeams.Values.SelectMany(t => t.Sources))
        {
          sources.Add(s.Name);
        }
        foreach (var s in playersForTeams.Values.SelectMany(tupleArray => tupleArray.SelectMany(p => p.Item1.Sources)))
        {
          sources.Add(s.Name);
        }
        sources.Add(Builtins.BuiltinSource.Name);
        sources.Add(Builtins.ManualSource.Name);

        try
        {
          foreach (var player in players)
          {
            placementsForPlayers[player.Id] = new Dictionary<string, Bracket[]>();
            foreach (var source in player.Sources)
            {
              placementsForPlayers[player.Id][source.Name] = source.Brackets;
            }
          }
        }
        catch (OutOfMemoryException oom)
        {
          const string message = "ERROR: OutOfMemoryException on PlacementsForPlayers. Will continue anyway.";
          Console.WriteLine(message);
          Console.WriteLine(oom.ToString());
          placementsForPlayers = new Dictionary<Guid, Dictionary<string, Bracket[]>>();
        }
      }

      // PROCESS SLAPP
      string title;
      Color colour;

      if (!hasPlayers && !hasTeams)
      {
        title = "Didn't find anything 😶";
        colour = Color.Red;
      }
      else if (!hasPlayers && hasTeams)
      {
        if (hasTeamsPl)
        {
          title = $"Found {teams.Length} teams!";
          colour = Color.Gold;
        }
        else
        {
          title = "Found a team!";
          colour = new Color(0xc27c0e);  // Dark gold
        }
      }
      else if (hasPlayers && !hasTeams)
      {
        if (hasPlayersPl)
        {
          title = $"Found {players.Length} players!";
          colour = Color.Blue;
        }
        else
        {
          title = "Found a player!";
          colour = Color.DarkBlue;
        }
      }
      else if (hasPlayers && hasTeams)
      {
        title = $"Found {players.Length} player{(hasPlayersPl ? "s" : "")} and {teams.Length} team{(hasTeamsPl ? "s" : "")}!";
        colour = Color.Green;
      }
      else
      {
        throw new NotImplementedException($"Slapp logic error players.Length={players.Length} teams.Length={teams.Length}");
      }

      var builder = EmbedUtility.ToEmbed("", colour, title);
      var reacts = new Dictionary<string, IReadonlySourceable>();  // Player or Team
      if (hasPlayers)
      {
        for (int i = 0; i < players.Length && i < DiscordLimits.MAX_EMBED_RESULTS; i++)
        {
          var player = players[i];
          try
          {
            await AddMatchedPlayer(builder, reacts, player);
          }
          catch (Exception ex)
          {
            builder.AddField("(Error Player)", ex.Message, false);
            log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
          }
        }
      }
      if (hasTeams)
      {
        for (int i = 0; i < teams.Length && i < DiscordLimits.MAX_EMBED_RESULTS; i++)
        {
          var team = teams[i];
          try
          {
            await AddMatchedTeam(builder, reacts, team);
          }
          catch (Exception ex)
          {
            builder.AddField("(Error Team)", ex.Message, false);
            log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
          }
        }
      }
      builder.WithFooter(Footer.GetRandomFooterPhrase() +
        (players.Length + teams.Length > DiscordLimits.MAX_EMBED_RESULTS ? $"Only the first {DiscordLimits.MAX_EMBED_RESULTS} results are shown for players and teams." : ""),
        iconUrl: "https://media.discordapp.net/attachments/471361750986522647/758104388824072253/icon.png");
      await Task.Yield();
      return (builder, colour, reacts);
    }

    private static Task AddMatchedPlayer(EmbedBuilder builder, Dictionary<string, IReadonlySourceable> reacts, Player player)
    {
      string fieldHead = player.Name.Value.Truncate(DiscordLimits.FIELD_NAME_LIMIT);
      string fieldBody = ("Sources: " + string.Join(", ", player.Sources.Select(s => s.Name))).Truncate(DiscordLimits.FIELD_VALUE_LIMIT);
      builder.AddField(fieldHead, fieldBody, false);
      return Task.CompletedTask;
    }

    private static Task AddMatchedTeam(EmbedBuilder builder, Dictionary<string, IReadonlySourceable> reacts, Team team)
    {
      string fieldHead = team.Name.Value.Truncate(DiscordLimits.FIELD_NAME_LIMIT);
      string fieldBody = ("Sources: " + string.Join(", ", team.Sources.Select(s => s.Name))).Truncate(DiscordLimits.FIELD_VALUE_LIMIT);
      builder.AddField(fieldHead, fieldBody, false);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Adds the player or team to the reacts dictionary. Returns the reaction that represents the addition,
    /// or null if there are no more reactions left in the NUMBERS_KEY_CAPS collection.
    /// </summary>
    private static string? AddToReactsDict(IDictionary<string, IReadonlySourceable> reacts, IReadonlySourceable playerOrTeam)
    {
      if (reacts.Count < numbersKeyCaps.Count)
      {
        string emojiNum = numbersKeyCaps[reacts.Count];
        reacts.Add(emojiNum, playerOrTeam);
        return emojiNum;
      }
      return null;
    }
  }
}