using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using SplatTagCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Mavis.SlappSupport
{
  internal class SlappCommandHandler
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
    private readonly SplatTagController splatTagController;

    public SlappCommandHandler(SplatTagController splatTagController)
    {
      this.splatTagController = splatTagController;
    }

    internal async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      if (splatTagController == null)
      {
        await command.RespondAsync(text: "⌛ Slapp not initialised yet.", ephemeral: true).ConfigureAwait(false);
        return;
      }

      Dictionary<string, object> commandParams = command.Data.Options.ToDictionary(kv => kv.Name, kv => kv.Value);
      log.Trace($"Processing Slapp Command with params: {string.Join(", ", commandParams.Select(kv => kv.Key + "=" + kv.Value))} ");

      SplatTagController.Verbose = commandParams.GetWithConversion("--verbose", false);
      string query = commandParams.GetWithConversion("query", "");

      var options = new MatchOptions
      {
        IgnoreCase = !commandParams.GetWithConversion("--exact-case", false),
        NearCharacterRecognition = !commandParams.GetWithConversion("--exact-character-recognition", false),
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

    private Task<(EmbedBuilder? builder, Color embedColour, Dictionary<string, IReadonlySourceable> reacts)>
      ProcessSlappCommand(string query, MatchOptions options)
    {
      var players = splatTagController.MatchPlayer(query, options);
      var teams = splatTagController.MatchTeam(query, options);
      var responseObject = new SlappResponseObject(players, teams, splatTagController);
      return ProcessSlapp(responseObject);
    }

    private async Task<(EmbedBuilder? builder, Color embedColour, Dictionary<string, IReadonlySourceable> reacts)> ProcessSlapp(SlappResponseObject r)
    {
      string title;
      Color colour;

      if (!r.HasPlayers && !r.HasTeams)
      {
        title = "Didn't find anything 😶";
        colour = Color.Red;
      }
      else if (!r.HasPlayers && r.HasTeams)
      {
        if (r.HasTeamsPl)
        {
          title = $"Found {r.teams.Length} teams!";
          colour = Color.Gold;
        }
        else
        {
          title = "Found a team!";
          colour = new Color(0xc27c0e);  // Dark gold
        }
      }
      else if (r.HasPlayers && !r.HasTeams)
      {
        if (r.HasPlayersPl)
        {
          title = $"Found {r.players.Length} players!";
          colour = Color.Blue;
        }
        else
        {
          title = "Found a player!";
          colour = Color.DarkBlue;
        }
      }
      else if (r.HasPlayers && r.HasTeams)
      {
        title = $"Found {r.players.Length} player{(r.HasPlayersPl ? "s" : "")} and {r.teams.Length} team{(r.HasTeamsPl ? "s" : "")}!";
        colour = Color.Green;
      }
      else
      {
        throw new NotImplementedException($"Slapp logic error players.Length={r.players.Length} teams.Length={r.teams.Length}");
      }

      var builder = EmbedUtility.ToEmbed("", colour, title);
      var reacts = new Dictionary<string, IReadonlySourceable>();  // Player or Team
      if (r.HasPlayers)
      {
        for (int i = 0; i < r.players.Length && i < DiscordLimits.MAX_EMBED_RESULTS; i++)
        {
          var player = r.players[i];
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
      if (r.HasTeams)
      {
        for (int i = 0; i < r.teams.Length && i < DiscordLimits.MAX_EMBED_RESULTS; i++)
        {
          var team = r.teams[i];
          try
          {
            await AddMatchedTeam(builder, reacts, r, team);
          }
          catch (Exception ex)
          {
            builder.AddField("(Error Team)", ex.Message, false);
            log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
          }
        }
      }
      builder.WithFooter(Footer.GetRandomFooterPhrase() +
        (r.players.Length + r.teams.Length > DiscordLimits.MAX_EMBED_RESULTS ? $"Only the first {DiscordLimits.MAX_EMBED_RESULTS} results are shown for players and teams." : ""),
        iconUrl: "https://media.discordapp.net/attachments/471361750986522647/758104388824072253/icon.png");
      await Task.Yield();
      return (builder, colour, reacts);
    }

    private Task AddMatchedPlayer(EmbedBuilder builder, Dictionary<string, IReadonlySourceable> reacts, Player player)
    {
      string fieldHead = player.Name.Value.Truncate(DiscordLimits.FIELD_NAME_LIMIT);
      string fieldBody = ("Sources: " + string.Join(", ", player.Sources.Select(s => s.Name))).Truncate(DiscordLimits.FIELD_VALUE_LIMIT);
      builder.AddField(fieldHead, fieldBody, false);
      return Task.CompletedTask;
    }

    private Task AddMatchedTeam(EmbedBuilder builder, Dictionary<string, IReadonlySourceable> reacts, SlappResponseObject r, Team team)
    {
      var groupedTeamSources = GetGroupedSourcesText(team);
      var players = r.playersForTeams[team.Id];
      var playersInTeam = players.Where(tup => tup.Item2).Select(tup => tup.Item1).ToArray();
      var playersEverInTeam = players.Select(tup => tup.Item1).ToArray();
      var playerStrings = new List<string>();
      var playerStringsDetailed = new List<string>();

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

    /// <summary>
    /// Group the SimpleSource list for the specified sources/team/player by tourney name.
    /// </summary>
    private ImmutableSortedDictionary<string, List<Source>> GetGroupedSources(IReadonlySourceable obj)
    {
      var sources = obj.Sources;
      var result = new Dictionary<string, List<Source>>();
      foreach (var source in sources)
      {
        result.AddOrAppend(source.StrippedTournamentName, source);
      }
      return result.ToImmutableSortedDictionary();
    }

    /// <summary>
    /// Group the SimpleSource list for the specified sources/team/player by tourney name.
    /// Duplicates are removed.
    /// </summary>
    private List<string> GetGroupedSourcesText(IReadonlySourceable obj)
    {
      var groups = GetGroupedSources(obj);
      List<string> message = new();
      foreach (var (tourney, simpleSources) in groups)
      {
        string groupKey = string.IsNullOrEmpty(tourney) ? "" : tourney + ": ";
        var groupValueArray = new HashSet<string>(string.IsNullOrEmpty(groupKey) ? simpleSources.Select(s => s.GetLinkedNameDisplay()) : simpleSources.Select(s => s.GetLinkedDateDisplay()));
        var orderedValues = new List<string>(groupValueArray).SortInline(reverse: true);
        var separator = string.IsNullOrEmpty(groupKey) ? "\n" : ", ";
        var groupValue = string.Join(separator, groupValueArray);
        message.Add($"{groupKey}{groupValue}");
      }
      return message;
    }
  }
}