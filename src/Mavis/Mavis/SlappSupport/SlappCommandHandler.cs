using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using SplatTagCore;
using SplatTagCore.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    private const string BATTLEFY = "<:battlefy:810346162161844274>";
    private const string DISCORD = "<:discord:810348495079866370>";
    private const string TWITCH = "<:twitch:810346162023432213>";
    private const string TWITTER = "<:twitter:810346162418614312>";
    private const string LOW_INK = "<:LowInk:869389316881809438>";
    private const string TYPING = "<a:typing:897094396502224916>";
    private const string EEVEE = "<a:eevee_slap:895391059985715241>";
    private const string TOP_500 = "<:Top500:896488842977230889>";
    private const string PLUS = "<:plus:916465605551460352>";

    #region Ability Constants

    private const string ABILITY_DOUBLER = "<:AbilityDoubler:841052609648525382>";
    private const string BOMB_DEFENSE_UP_DX = "<:BombDefenseUpDX:841052609333821471>";
    private const string COMEBACK = "<:Comeback:841052609300922419>";
    private const string DROP_ROLLER = "<:DropRoller:841052609859289118>";
    private const string HAUNT = "<:Haunt:841052609850900530>";
    private const string INK_RECOVERY_UP = "<:InkRecoveryUp:841052609866629120>";
    private const string INK_RESISTANCE_UP = "<:InkResistanceUp:841052609627684875>";
    private const string INK_SAVER_MAIN = "<:InkSaverMain:841052609875148911>";
    private const string INK_SAVER_SUB = "<:InkSaverSub:841052609992327168>";
    private const string LAST_DITCH_EFFORT = "<:LastDitchEffort:841052609870692362>";
    private const string MAIN_POWER_UP = "<:MainPowerUp:841052609972011008>";
    private const string NINJA_SQUID = "<:NinjaSquid:841052610122219574>";
    private const string OBJECT_SHREDDER = "<:ObjectShredder:841052609858895914>";
    private const string OPENING_GAMBIT = "<:OpeningGambit:841052610080669697>";
    private const string QUICK_RESPAWN = "<:QuickRespawn:841052610256437289>";
    private const string QUICK_SUPER_JUMP = "<:QuickSuperJump:841052609723891733>";
    private const string RESPAWN_PUNISHER = "<:RespawnPunisher:841052610050916383>";
    private const string RUN_SPEED_UP = "<:RunSpeedUp:841052610320400475>";
    private const string SPECIAL_CHARGE_UP = "<:SpecialChargeUp:841052610193784862>";
    private const string SPECIAL_POWER_UP = "<:SpecialPowerUp:841052610199027764>";
    private const string SPECIAL_SAVER = "<:SpecialSaver:841052610038726667>";
    private const string STEALTH_JUMP = "<:StealthJump:841052610131656724>";
    private const string SUB_POWER_UP = "<:SubPowerUp:841052609912897567>";
    private const string SWIM_SPEED_UP = "<:SwimSpeedUp:841052610106097674>";
    private const string TENACITY = "<:Tenacity:841052610173206548>";
    private const string THERMAL_INK = "<:ThermalInk:841052609803583489>";
    private const string UNKNOWN_ABILITY = "<:Unknown:892498504822431754>";

    #endregion Ability Constants

    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly SplatTagController splatTagController;
    private readonly Dictionary<ulong, IDictionary<IEmote, IReadonlySourceable>> slappReactsQueue = new();

    public SlappCommandHandler(SplatTagController splatTagController)
    {
      this.splatTagController = splatTagController;
    }

    internal async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      if (splatTagController == null)
      {
        await command.ModifyOriginalResponseAsync((message) => message.Content = "⌛ Slapp not initialised yet.").ConfigureAwait(false);
        return;
      }

      Dictionary<string, object> commandParams = command.Data.Options.ToDictionary(kv => kv.Name, kv => kv.Value);
      log.Info($"Processing Slapp Command {command.AsCommandString()}");
      
      bool verbose = commandParams.GetWithConversion("--verbose", false);
      if (verbose)
      {
        SplatTagDatabase.SplatTagControllerFactory.SetNLogLevel(LogLevel.Trace);
      }
      else
      {
        SplatTagDatabase.SplatTagControllerFactory.SetNLogLevel(LogLevel.Info);
      }
      string query = commandParams.GetWithConversion("query", "");

      var options = new MatchOptions
      {
        IgnoreCase = !commandParams.GetWithConversion("--exact-case", false),
        NearCharacterRecognition = !commandParams.GetWithConversion("--exact-character-recognition", false),
        QueryIsRegex = commandParams.GetWithConversion("--query-is-regex", false),
        Limit = commandParams.GetWithConversion("--limit", 20),
      };

      // If this is a friend code query
      if (query.StartsWith("SW-", StringComparison.OrdinalIgnoreCase))
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

      if (commandParams.GetWithConversion("--query-is-player", false))
      {
        options.FilterOptions = FilterOptions.Player;
      }

      if (commandParams.GetWithConversion("--query-is-team", false))
      {
        options.FilterOptions = FilterOptions.Team;
      }

      if (commandParams.GetWithConversion("--query-is-clan-tag", false))
      {
        options.FilterOptions = FilterOptions.ClanTag;
      }

      log.Trace($"Building result for {query}.");

      var originalMessage = await command.ModifyOriginalResponseAsync((message) => message.Content = $"{PleaseWaitMessages.GetRandomMessage()} {(splatTagController.CachingDone ? "🏃‍" : "(Caching not yet done, will take a little longer 🐢!)")}");

      _ = Task.Run(async () =>
      {
        try
        {
          // Process the command
          var (builder, reacts) = await ProcessSlappCommand(query, options);

          // Build the split responses from the builder
          Discord.Rest.RestFollowupMessage? lastMessageSent = null;
          foreach (var embed in builder.SmartBuild())
          {
            // Send the responses
            lastMessageSent = await command.FollowupAsync(
              text: command.AsCommandString(),
              embed: embed,
              ephemeral: false)
            .ConfigureAwait(false);
          }

          // Now we're at the end, react to the last message (and only do so if we've sent a message)
          if (lastMessageSent != null)
          {
            AddReactionsToMessage(lastMessageSent, reacts);
          }
        }
        catch (Exception ex)
        {
          string errorMessage = "Something went wrong processing the result from Slapp. Blame Slate. 😒🤔" + ex.Message;
          await command.FollowupAsync(
           embed: new MavisEmbedBuilder(errorMessage, Color.Red).BuildFirst(),
           ephemeral: false)
          .ConfigureAwait(false);
          log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
          return;
        }

        try
        {
          // Delete the loading message
          await originalMessage.DeleteAsync();
        }
        catch (Exception ex)
        {
          log.Debug($"Exception deleting the original response to {command.Data.Name}. {ex.Message}");
          log.Trace(ex);
        }

        log.Trace($"Finished {query} processing");
      }).ConfigureAwait(false);
    }

    internal async Task HandleReaction(IUserMessage messageContext, Cacheable<IMessageChannel, ulong> channelContext, SocketReaction reaction)
    {
      var messageReactions = this.slappReactsQueue.GetValueOrDefault(messageContext.Id);
      bool handled = false;
      if (messageReactions?.TryGetValue(reaction.Emote, out var response) == true)
      {
        log.Info($"Reaction received matching message Id={messageContext.Id}, reaction.Emote.Name={reaction.Emote.Name}");
        Player[] players = response is Player player ? new[] { player } : Array.Empty<Player>();
        Team[] teams = response is Team team ? new[] { team } : Array.Empty<Team>();
        var r = new SlappResponseObject(players, teams, splatTagController);

        try
        {
          var (builder, reacts) = await ProcessSlappResponse(r);

          // Build the split responses from the builder
          IUserMessage? lastMessageSent = null;
          foreach (var embed in builder.SmartBuild())
          {
            // Send the responses
            lastMessageSent = await messageContext.ReplyAsync(
              embed: embed)
            .ConfigureAwait(false);
          }

          // Now we're at the end, react to the last message (and only do so if we've sent a message)
          if (lastMessageSent != null)
          {
            AddReactionsToMessage(lastMessageSent, reacts);
          }
        }
        catch (Exception ex)
        {
          string errorMessage = "Something went wrong processing the reaction. Blame Slate. 😒🤔" + ex.Message;
          await messageContext.ReplyAsync(
           embed: new MavisEmbedBuilder(errorMessage, Color.Red).BuildFirst())
          .ConfigureAwait(false);
          log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
        }
        handled = true;
      }
      else
      {
        log.Warn($"Discarding react against message={messageReactions}, Id={messageContext.Id}, reaction.Emote.Name={reaction.Emote.Name}");
      }

      if (handled)
      {
        this.slappReactsQueue[messageContext.Id].Remove(reaction.Emote);
      }
    }

    private void AddReactionsToMessage(IUserMessage lastMessageSent, Dictionary<IEmote, IReadonlySourceable> reacts)
    {
      // Record into the buffer
      this.slappReactsQueue[lastMessageSent.Id] = reacts;

      // Keep a rolling limit of x messages' data
      if (this.slappReactsQueue.Count > 50)
      {
        // Take the minimum key by id (this should be the earliest in time).
        this.slappReactsQueue.Remove(this.slappReactsQueue.Min(pair => pair.Key));
      }

      Task.Run(async () =>
      {
        foreach (var r in reacts)
        {
          try
          {
            await lastMessageSent.AddReactionAsync(r.Key);
          }
          catch (Exception ex)
          {
            log.Error($"Failed to add a reaction r.key={r.Key}. ex={ex}");
          }
        }
      });
    }

    /// <summary>
    /// Process the Slapp query string and parsed options.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private Task<(MavisEmbedBuilder builder, Dictionary<IEmote, IReadonlySourceable> reacts)>
      ProcessSlappCommand(string query, MatchOptions options)
    {
      var players = splatTagController.MatchPlayer(query, options);
      var teams = splatTagController.MatchTeam(query, options);
      var responseObject = new SlappResponseObject(players, teams, splatTagController);
      return ProcessSlappResponse(responseObject);
    }

    /// <summary>
    /// Process the Slapp Response and return the awaitable task for the resultant builder and reactions for that message.
    /// </summary>
    /// <param name="r">The constructed SlappResponseObject from Slapp</param>
    private async Task<(MavisEmbedBuilder builder, Dictionary<IEmote, IReadonlySourceable> reacts)> ProcessSlappResponse(SlappResponseObject r)
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
          title = $"Found {r.Teams.Count} teams!";
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
        title = $"Found {r.players.Length} {"player".Plural(r.players)} and {r.Teams.Count} {"team".Plural(r.Teams)}!";
        colour = Color.Green;
      }
      else
      {
        throw new NotImplementedException($"Slapp logic error players.Length={r.players.Length} teams.Length={r.Teams.Count}");
      }

      var builder = new MavisEmbedBuilder().WithColor(colour).WithAuthor(title);
      var reactsForMessage = new Dictionary<IEmote, IReadonlySourceable>();  // Player or Team
      if (r.HasPlayers)
      {
        for (int i = 0; i < r.players.Length && i < DiscordLimits.MAX_EMBED_RESULTS; i++)
        {
          var player = r.players[i];
          try
          {
            await AddMatchedPlayer(builder, reactsForMessage, r, player);
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
        foreach (Team team in r.Teams.Take(DiscordLimits.MAX_EMBED_RESULTS))
        {
          try
          {
            await AddMatchedTeam(builder, reactsForMessage, r, team);
          }
          catch (Exception ex)
          {
            builder.AddField("(Error Team)", ex.Message, false);
            log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
          }
        }
      }
      builder.WithFooter(Footer.GetRandomFooterPhrase() +
        (r.players.Length + r.Teams.Count > DiscordLimits.MAX_EMBED_RESULTS ? $"Only the first {DiscordLimits.MAX_EMBED_RESULTS} results are shown for players and teams." : ""),
        iconUrl: "https://media.discordapp.net/attachments/471361750986522647/758104388824072253/icon.png");
      await Task.Yield();
      return (builder, reactsForMessage);
    }

    private static async Task AddMatchedPlayer(MavisEmbedBuilder builder, Dictionary<IEmote, IReadonlySourceable> reacts, SlappResponseObject r, Player player)
    {
      // Transform names by adding a backslash to any backslashes.
      string[] names = player.Names.Where(n => !string.IsNullOrEmpty(n?.Value)).Select(n => n.Value.EscapeCharacters()).Distinct().ToArray();
      string currentName = (names.FirstOrDefault() ?? "(Unnamed Player)").SafeBackticks();
      CalculateAddMatchedPlayerTeams(r, player, reacts, out string currentTeam, out List<string> oldTeams);

      string otherNames = (names.Length > 1)
        ? string.Join("\n", names.Skip(1).Select(n => n.Truncate(256)))
          .ConditionalString(prefix: "_ᴬᴷᴬ_ ```", suffix: "```\n")
          .Truncate(1000, "…\n```\n")
        : "";

      string[] battlefy = player.BattlefySlugsOrdered.Select(profile => $"{BATTLEFY} [{profile.Value.EscapeCharacters()}]({profile.Uri})").ToArray();
      string[] discord = player.DiscordIds.Select(profile =>
      {
        var did = profile.Value.EscapeCharacters();
        return $"{DISCORD} [{did}](https://discord.id/?prefill={did}) \n🦑 [Sendou](https://sendou.ink/u/{did})";
      }).ToArray();
      string[] twitch = player.TwitchProfiles.Select(profile => $"{TWITCH} [{profile.Value.EscapeCharacters()}]({profile.Uri})").ToArray();
      string[] twitter = player.TwitterProfiles.Select(profile => $"{TWITTER} [{profile.Value.EscapeCharacters()}]({profile.Uri})").ToArray();
      string countryFlag = (player.CountryFlag + " ").Or("");
      string top500 = player.Top500 ? (TOP_500 + " ") : "";
      string[] notableResults = GetFirstPlacementsText(r, player).ToArray();
      string? lowInkPosStr = GetWinningLowInkPosString(r, player);
      string fieldHead = countryFlag + top500 + currentName;

      // Single player detailed view --
      // If there's just the one matched player, move the extras to another field.
      if (r.players.Length == 1 && r.Teams.Count < 14)
      {
        builder.AddField(fieldHead, otherNames, defaultName: "(Unnamed Player)", defaultValue: "(No other names)");

        var fcsLength = player.FCs.Count;
        builder.AddField("FCs:", $"{fcsLength} known friend code".Plural(fcsLength));

        if (currentTeam?.Length > 0)
        {
          builder.AddField("Current team:", currentTeam);
        }

        builder.ConditionallyAddUnrolledList("Old teams", oldTeams);
        builder.ConditionallyAddUnrolledList("Twitch", twitch);
        builder.ConditionallyAddUnrolledList("Twitter", twitter);
        builder.ConditionallyAddUnrolledList("Battlefy", battlefy);
        builder.ConditionallyAddUnrolledList("Discord", discord);

        if (notableResults.Length > 0 || player.PlusMembership.Count > 0 || lowInkPosStr is not null)
        {
          var notableResultLines = new List<string>();
          notableResultLines.AddRange(player.PlusMembership.OrderByDescending(plus => plus.Date).Select(plus => $"{PLUS} +{plus.Level} member ({plus.Date:MMM yyyy})"));

          if (lowInkPosStr is not null)
          {
            notableResultLines.Add(lowInkPosStr);
          }

          notableResultLines.AddRange(notableResults.Select(result => $"🏆 Won {result}"));

          builder.AddUnrolledList("Notable Wins", notableResultLines);
        }

        builder.ConditionallyAddUnrolledList("Weapons", player.Weapons.ToArray(), separator: ", ");

        // Add Skill/Clout here

        builder.AddUnrolledList("Sources", GetGroupedSourcesText(player));
      }

      // Multiple players summary view --
      else
      {
        var emojiNum = AddToReactsDict(reacts, player);
        var additionalInfo = emojiNum != null ? $"\n React {emojiNum} for more…\n" : $"\n More info: /full {player.Id}\n";
        string notableResultsStr = "";
        if (notableResults.Length > 0 || player.PlusMembership.Count > 0 || lowInkPosStr is not null)
        {
          var latestPlus = player.PlusMembership.OrderByDescending(plus => plus.Date).FirstOrDefault();
          notableResultsStr += latestPlus == null ? "" : $"{PLUS} +{latestPlus.Level} member ({latestPlus.Date:MMM yyyy})\n";

          if (lowInkPosStr is not null)
          {
            notableResultsStr += lowInkPosStr + "\n";
          }

          notableResultsStr += string.Join("\n", notableResults.Select(result => $"🏆 Won {result}")).ConditionalString(suffix: "\n");
        }

        int fcsLength = player.FCs.Count;
        string fcsStr = fcsLength > 0 ? ($"{fcsLength} known friend code".Plural(fcsLength) + "\n") : "";
        string oldTeamsStr = string.Join("\n", oldTeams)
          .ConditionalString(prefix: "Old teams:\n", suffix: "\n")
          .Truncate(3 * DiscordLimits.FIELD_VALUE_LIMIT / 4)
          .CloseBackticksIfUnclosed();

        string socialsStr =
          string.Join("\n", twitch).ConditionalString(suffix: "\n") +
          string.Join("\n", twitter).ConditionalString(suffix: "\n") +
          string.Join("\n", battlefy).ConditionalString(suffix: "\n") +
          string.Join("\n", discord).ConditionalString(suffix: "\n")
          .Truncate(DiscordLimits.FIELD_VALUE_LIMIT)
          .ConditionalString(suffix: "\n");

        var fieldBody = (otherNames + currentTeam + oldTeamsStr + fcsStr + socialsStr + notableResultsStr).Or("(Nothing else to say)\n");
        fieldBody += "Sources:\n" + string.Join("\n", GetGroupedSourcesText(player));

        if (fieldBody.Length + additionalInfo.Length >= DiscordLimits.FIELD_VALUE_LIMIT)
        {
          fieldBody = fieldBody.Truncate(DiscordLimits.FIELD_VALUE_LIMIT - 4 - additionalInfo.Length).CloseBackticksIfUnclosed();
        }
        fieldBody += additionalInfo;
        builder.AddField(fieldHead, fieldBody, defaultName: "(Unnamed Player)");
        await Task.Yield();
      }
    }

    private static string? GetWinningLowInkPosString(SlappResponseObject r, Player player)
    {
      var bestLowInk = r.GetBestLowInkPlacement(player);
      int? winningLowInkPos = bestLowInk != null ?
        ((bestLowInk.Value.b.Name.Contains("Top Cut") || bestLowInk.Value.b.Name.Contains("Alpha")) ? bestLowInk.Value.place : null)
        : null;

      string? lowInkPosStr = null;
      if (winningLowInkPos is not null)
      {
        if (winningLowInkPos == 1)
        {
          lowInkPosStr = $"{LOW_INK} Low Ink Winner";
        }
        else if (winningLowInkPos == 2)
        {
          lowInkPosStr = $"{LOW_INK} Low Ink 🥈";
        }
        else if (winningLowInkPos == 3)
        {
          lowInkPosStr = $"{LOW_INK} Low Ink 🥉";
        }
      }

      return lowInkPosStr;
    }

    private static void CalculateAddMatchedPlayerTeams(SlappResponseObject r, Player player, Dictionary<IEmote, IReadonlySourceable> reacts, out string currentTeam, out List<string> oldTeams)
    {
      // Current and old teams
      (Team teamForPlayer, ReadOnlyCollection<Source> teamSources)[] resolvedTeams = r.GetTeamsForPlayer(player);
      currentTeam = "";
      oldTeams = new();
      if (resolvedTeams.Length > 0)
      {
        const int TOURNEYS_TO_TAKE_FOR_PLAYER = 3;
        StringBuilder currentTeamBuilder = new();

        if (r.players.Length == 1)
        {
          var emojiNum = AddToReactsDict(reacts, resolvedTeams[0].teamForPlayer);
          if (emojiNum != null)
          {
            currentTeamBuilder.Append("Plays for:\n").Append(emojiNum).Append(' ').Append(resolvedTeams[0].teamForPlayer.ToString().WrapInBackticks()).Append('\n');
          }

          var tournamentAppearancesForThisPlayer = resolvedTeams[0].teamSources.Where(s => s.Players.Contains(player)).OrderByDescending(s => s).Select(s => s.GetLinkedNameDisplay()).ToArray();
          if (tournamentAppearancesForThisPlayer.Length > 0)
          {
            currentTeamBuilder.AppendJoin(", ", tournamentAppearancesForThisPlayer.Take(TOURNEYS_TO_TAKE_FOR_PLAYER));

            bool andMore = tournamentAppearancesForThisPlayer.Length > TOURNEYS_TO_TAKE_FOR_PLAYER;
            int andMoreCount = tournamentAppearancesForThisPlayer.Length - TOURNEYS_TO_TAKE_FOR_PLAYER;
            currentTeamBuilder.Append(andMore ? $" +{andMoreCount} other tourneys…" : "");
          }
        }

        if (currentTeamBuilder.Length == 0)
        {
          currentTeamBuilder.Append("Plays for: ").Append(resolvedTeams[0].teamForPlayer.ToString().WrapInBackticks()).Append('\n');
        }
        currentTeam = currentTeamBuilder.ToString();

        // Old teams
        if (resolvedTeams.Length > 1)
        {
          var resolvedOldTeams = resolvedTeams.Skip(1);

          // Add reacts and tournament entries if for a single player entry
          if (r.players.Length == 1)
          {
            foreach (var (teamForPlayer, teamSources) in resolvedOldTeams)
            {
              StringBuilder sb = new();
              var tournamentAppearancesForThisPlayer = teamSources.Where(s => s.Players.Contains(player)).OrderByDescending(s => s).Select(s => s.GetLinkedNameDisplay()).ToArray();

              var emojiNum = AddToReactsDict(reacts, teamForPlayer);
              if (emojiNum != null)
              {
                sb.Append(emojiNum).Append(' ');
              }
              sb.Append(teamForPlayer.ToString().WrapInBackticks()).Append(' ');

              if (tournamentAppearancesForThisPlayer.Length > 0)
              {
                sb.AppendJoin(", ", tournamentAppearancesForThisPlayer.Take(TOURNEYS_TO_TAKE_FOR_PLAYER));

                bool andMore = tournamentAppearancesForThisPlayer.Length > TOURNEYS_TO_TAKE_FOR_PLAYER;
                int andMoreCount = tournamentAppearancesForThisPlayer.Length - TOURNEYS_TO_TAKE_FOR_PLAYER;
                sb.Append(andMore ? $" +{andMoreCount} other tourneys…" : "");
              }
              oldTeams.Add(sb.ToString());
            }
          }
          else
          {
            oldTeams.AddRange(resolvedOldTeams.Select(oldT => oldT.teamForPlayer.ToString().WrapInBackticks()));
          }
        }
      }
    }

    private async Task AddMatchedTeam(MavisEmbedBuilder builder, Dictionary<IEmote, IReadonlySourceable> reacts, SlappResponseObject r, Team team)
    {
      var groupedTeamSources = GetGroupedSourcesText(team);
      var players = splatTagController.GetPlayersForTeam(team);
      var playersInTeam = new List<Player>();
      var playersEverInTeam = new List<Player>();
      var playerStrings = new List<string>();
      var playerStringsDetailed = new List<string>();
      foreach (var playerTuple in players)
      {
        const int NAMES_TO_TAKE = 9;
        var p = playerTuple.player;
        var inTeam = playerTuple.mostRecent;
        string name = $"{p.Name.Value.Truncate(48).SafeBackticks()}";
        string aka = string.Join(", ", p.Names.Skip(1).Take(NAMES_TO_TAKE).Select(n => n.Value.Truncate(20))).ConditionalString(prefix: "_ᴬᴷᴬ_ ");
        bool andMore = p.Names.Count > (1 + NAMES_TO_TAKE);
        int andMoreCount = andMore ? p.Names.Count - (1 + NAMES_TO_TAKE) : 0;
        playerStrings.Add(name);
        playerStringsDetailed.Add($"{(inTeam ? "_(Latest)_" : "_(Ex)_")} {name} {aka}{(andMore ? $" +{andMoreCount} other names…" : "")}");
        playersEverInTeam.Add(p);
        if (inTeam)
        {
          playersInTeam.Add(p);
        }
      }
      var divPhrase = team.GetBestTeamPlayerDivString(splatTagController).ConditionalString(suffix: "\n");

      // Single team detailed view.
      // If there's just the one matched team, move the sources to the next field.
      if (r.Teams.Count == 1)
      {
        // Add in emoji reacts
        for (int j = 0; j < playerStringsDetailed.Count; j++)
        {
          var emojiNum = AddToReactsDict(reacts, playersEverInTeam[j]);
          if (emojiNum != null)
          {
            playerStringsDetailed[j] = emojiNum + " " + playerStringsDetailed[j];
          }
        }

        // Team details
        var tagsStr = team.ClanTags.Count > 0 ? "Tags: " + string.Join(", ", team.ClanTags.Select(tag => tag.Value.SafeBackticks())) + "\n" : "";
        var numPlayersStr = " " + players.Count + " player".Plural(players);
        builder.AddField(
          name: team.ToString(),
          value: divPhrase + tagsStr + numPlayersStr,
          defaultName: "(Unnamed Team)"
        );

        // Show team's alternate if any
        if (team.Names.Count > 1)
        {
          builder.AddField(
            name: "Other names:",
            value: string.Join(", ", team.Names.Select(n => n.Value.SafeBackticks())),
            inline: false
            );
        }

        // Iterate through the team's players up to a maximum of 10 fields
        string[] source = playerStringsDetailed.ToArray();
        for (int j = 0; j < 10; j++)
        {
          const int BATCH = 5;
          int startIndex = j * BATCH;
          var splice = playerStringsDetailed.Skip(startIndex).Take(BATCH);
          if (splice.Any())
          {
            builder.AddField(
              name: $"Players ({j + 1}):",
              value: string.Join("\n", splice),
              inline: false);
          }
          else
          {
            break;
          }
        }

        // Insert Skills and Clout here ...

        builder.ConditionallyAddField(
          name: BATTLEFY + " Battlefy Uri:",
          value: team.BattlefyPersistentTeamId?.Uri?.ToString(),
          inline: false
        );

        builder.AddField(
          name: "Slapp Id:",
          value: team.Id.ToString(),
          inline: false
        );

        builder.AddUnrolledList("Sources", groupedTeamSources);
      }
      // Multiple teams summary view --
      else
      {
        var emojiNum = AddToReactsDict(reacts, team);
        var additionalInfo = emojiNum != null ? $"\n React {emojiNum} for more…\n" : $"\n More info: /full {team.Id}\n";
        var fieldBody = $"{divPhrase}Players:\n{string.Join(", ", playerStrings)}\n";
        var sourcesField = "Sources:\n" + string.Join("\n", groupedTeamSources);
        fieldBody += sourcesField;
        if (fieldBody.Length + additionalInfo.Length < DiscordLimits.FIELD_VALUE_LIMIT)
        {
          fieldBody += additionalInfo;
        }
        else
        {
          fieldBody = fieldBody.Truncate(DiscordLimits.FIELD_VALUE_LIMIT - 4 - additionalInfo.Length);
          fieldBody = fieldBody.CloseBackticksIfUnclosed();
          fieldBody += additionalInfo;
        }

        builder.AddField(
          name: team.ToString(),
          value: fieldBody,
          defaultName: "(Unnamed Team)"
        );
        await Task.Yield();
      }
    }

    /// <summary>
    /// Adds the player or team to the reacts dictionary. Returns the reaction that represents the addition,
    /// or null if there are no more reactions left in the NUMBERS_KEY_CAPS collection.
    /// </summary>
    private static string? AddToReactsDict(IDictionary<IEmote, IReadonlySourceable> reacts, IReadonlySourceable playerOrTeam)
    {
      if (reacts.Count < numbersKeyCaps.Count)
      {
        string keycapEmoteStr = numbersKeyCaps[reacts.Count];
        reacts.Add(keycapEmoteStr.ToEmote(), playerOrTeam);
        return keycapEmoteStr;
      }
      return null;
    }

    /// <summary>
    /// Group the SimpleSource list for the specified sources/team/player by tourney name.
    /// Keyed by a group key which roughly relates to the sources (e.g. the sources' beginning stripped names which may include the organisation)
    /// </summary>
    internal static ImmutableSortedDictionary<string, List<Source>> GetGroupedSources(IReadonlySourceable obj)
    {
      var sources = obj.Sources;
      var result = new Dictionary<string, List<Source>>();

      foreach (var source in sources)
      {
        string name = source.StrippedTournamentName;
        var separatorIndexes = name.IndexOfAll('-');
        if (separatorIndexes.Count > 0)
        {
          List<string> majorityNames = new();
          // reverse order, missing the first and last parts for substrings. If there are 2 or fewer hyphens, no names are made.
          for (int i = separatorIndexes.Count - 1; i > 0; i--)
          {
            string majorityName = name[..separatorIndexes[i]];
            majorityNames.Add(majorityName);
          }

          string? existingGroupKey = null;
          string? bestMajorityName = null;
          foreach (string majorityName in majorityNames)
          {
            existingGroupKey = result.Keys.FirstOrDefault(group => majorityName.Length > group.Length ? majorityName.StartsWith(group) : group.StartsWith(majorityName));
            if (existingGroupKey != null)
            {
              bestMajorityName = majorityName;
              break;
            }
          }

          if (existingGroupKey == null)
          {
            result.AddOrAppend(name, source); // Add the full name rather than the majority name
          }
          else
          {
            // If we need to migrate the group
            Debug.Assert(bestMajorityName != null, "Expected bestMajorityName set if existingGroupKey is not null.");
            if (bestMajorityName.Length < existingGroupKey.Length)
            {
              var existingSources = result[existingGroupKey];
              existingSources.Add(source);
              result.Remove(existingGroupKey);
              result[bestMajorityName] = existingSources;
            }
            else
            {
              // Nope, just add the new source to this group
              result[existingGroupKey].Add(source);
            }
          }
        }
        else
        {
          // Can't work out a group as there's no dashes - just add in the stripped tourney name.
          result.AddOrAppend(source.StrippedTournamentName, source);
        }
      }
      return result.ToImmutableSortedDictionary();
    }

    /// <summary>
    /// Group the SimpleSource list for the specified sources/team/player by tourney name.
    /// Duplicates are removed.
    /// </summary>
    internal static List<string> GetGroupedSourcesText(IReadonlySourceable obj)
    {
      var groups = GetGroupedSources(obj);
      List<string> message = new();
      foreach (var (tourneyGroupName, simpleSources) in groups)
      {
        string groupKey = string.IsNullOrEmpty(tourneyGroupName) ? "" : tourneyGroupName + ": ";
        var groupValueArray = new HashSet<string>(string.IsNullOrEmpty(groupKey) ? simpleSources.Select(s => s.GetLinkedNameDisplay()) : simpleSources.Select(s => s.GetLinkedDateDisplay()));
        var orderedValues = new List<string>(groupValueArray).SortInline(reverse: true);
        var separator = string.IsNullOrEmpty(groupKey) ? "\n" : ", ";
        var groupValue = string.Join(separator, groupValueArray);
        message.Add(groupKey + groupValue);
      }
      return message;
    }

    /// <summary>
    /// Gets a list of displayed text in form where the specified player has come first.
    /// </summary>
    private static IEnumerable<string> GetFirstPlacementsText(SlappResponseObject r, Player p)
    {
      return r.GetPlacementsByPlace(p).Select(tup =>
        $"{tup.b.Name} in {tup.s.GetLinkedNameDisplay()} " +
        string.Join(" or ", p.Names.Where(n => n.Sources.Contains(tup.s)).Select(n => n.Value.Truncate(16).SafeBackticks())).ConditionalString(prefix: "as ") + " " +
        tup.t?.ToString().ConditionalString(prefix: "for team ")?.Truncate(64).SafeBackticks()
      );
    }
  }
}