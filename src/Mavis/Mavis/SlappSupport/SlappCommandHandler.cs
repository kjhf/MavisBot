using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using SplatTagCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private readonly Dictionary<ulong, IDictionary<string, IReadonlySourceable>> slappReactsQueue = new();

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
        Limit = commandParams.GetWithConversion("--limit", 20),
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

      await command.RespondAsync(text: "<a:typing:897094396502224916> Just a sec!", ephemeral: true);
      _ = Task.Run(async () =>
      {
        try
        {
          var (embed, colour, reacts) = await ProcessSlappCommand(query, options);

          if (embed == null)
          {
            await command.FollowupAsync(
              text: "Something went wrong processing the result from Slapp: no embed was made. 😔",
              ephemeral: false)
              .ConfigureAwait(false);
            return;
          }

          // Might need to do some truncation and moving fields to another message here.

          // Now we're at the end, react to the last message (and only do so if we've sent a message)
          var lastMessageSent = await command.FollowupAsync(
            embed: embed.WithColor(colour).Build(),
            ephemeral: false)
          .ConfigureAwait(false);

          await lastMessageSent.AddReactionsAsync(reacts.Select(r => r.Key.ToEmote()).ToArray());
          // And record into the buffer
          AddToReactsBuffer(lastMessageSent.Id, reacts);
        }
        catch (Exception ex)
        {
          string errorMessage = "Something went wrong processing the result from Slapp. Blame Slate. 😒🤔" + ex.Message;
          await command.FollowupAsync(
           embed: EmbedUtility.ToEmbed(errorMessage, Color.Red).Build(),
           ephemeral: false)
          .ConfigureAwait(false);
          log.Error(ex, $"<@!97288493029416960> {ex}"); // @Slate in logging channel
          return;
        }

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

    internal async Task HandleReaction(Cacheable<IUserMessage, ulong> messageContext, Cacheable<IMessageChannel, ulong> channelContext, SocketReaction reaction)
    {
      var channel = channelContext.Value;
      var message = this.slappReactsQueue.GetValueOrDefault(messageContext.Id);
      bool handled = false;
      if (message?.TryGetValue(reaction.Emote.Name, out var response) == true)
      {
        log.Info($"Reaction received matching message Id={messageContext.Id}, reaction.Emote.Name={reaction.Emote.Name}");
        // Add to queue, handle the new request ... (use full)
        await Task.Yield();
        handled = true;
      }
      else
      {
        log.Warn($"Something went wrong with handling the react: message={message}, Id={messageContext.Id}, reaction.Emote.Name={reaction.Emote.Name}");
      }

      if (handled)
      {
        this.slappReactsQueue[messageContext.Id].Remove(reaction.Emote.Name);
      }
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
        title = $"Found {r.players.Length} {"player".Plural(r.players)} and {r.teams.Length} {"team".Plural(r.teams)}!";
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
            await AddMatchedPlayer(builder, reacts, r, player);
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

    private async Task AddMatchedPlayer(EmbedBuilder builder, Dictionary<string, IReadonlySourceable> reacts, SlappResponseObject r, Player player)
    {
      const int TOURNEYS_TO_TAKE_FOR_PLAYER = 2;

      // Transform names by adding a backslash to any backslashes.
      string[] names = player.Names.Where(n => !string.IsNullOrEmpty(n?.Value)).Select(n => n.Value.EscapeCharacters()).Distinct().ToArray();
      string currentName = (names.FirstOrDefault() ?? "(Unnamed Player)").SafeBackticks();
      IReadOnlyList<(Team teamForPlayer, IReadOnlyList<Source> teamSources)>? resolvedTeams = r.GetTeamsForPlayer(player);

      StringBuilder currentTeamBuilder = new();
      if (r.players.Length == 1 && resolvedTeams.Count > 0)
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

      if (currentTeamBuilder.Length == 0 && resolvedTeams.Count > 0)
      {
        currentTeamBuilder.Append("Plays for: ").Append(resolvedTeams[0].teamForPlayer.ToString().WrapInBackticks()).Append('\n');
      }

      string currentTeam = currentTeamBuilder.ToString();

      // Old teams
      var oldTeams = new List<string>();
      if (resolvedTeams.Count > 1)
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

      string otherNames = "";
      if (names.Length > 1)
      {
        otherNames =
          string.Join("\n", names.Skip(1).Select(n => n.Truncate(256)))
          .ConditionalString(prefix: "_ᴬᴷᴬ_ ```", suffix: "```\n")
          .Truncate(1000, "…\n```\n");
      }

      string[] battlefy = player.Battlefy.Slugs.Select(profile => $"{BATTLEFY} [{profile.Value.EscapeCharacters()}]({profile.Uri})").ToArray();
      string[] discord = player.DiscordIds.Select(profile =>
      {
        var did = profile.Value.EscapeCharacters();
        return $"{DISCORD} [{did}](https://discord.id/?prefill={did}) \n🦑 [Sendou](https://sendou.ink/u/{did})";
      }).ToArray();
      string[] twitch = player.Twitch.Select(profile => $"{TWITCH} [{profile.Value.EscapeCharacters()}]({profile.Uri})").ToArray();
      string[] twitter = player.Twitter.Select(profile => $"{TWITTER} [{profile.Value.EscapeCharacters()}]({profile.Uri})").ToArray();
      string countryFlag = (player.CountryFlag + " ").Or("");
      string top500 = player.Top500 ? (TOP_500 + " ") : "";
      string fieldHead = countryFlag + top500 + currentName.Truncate(DiscordLimits.FIELD_NAME_LIMIT).Or("(Unnamed Player)");
      string[] notableResults = GetFirstPlacementsText(r, player).ToArray();
      var bestLowInk = r.GetBestLowInkPlacement(player);
      int? winningLowInkPos = bestLowInk != null ?
        ((bestLowInk.Value.b.Name.Contains("Top Cut") || bestLowInk.Value.b.Name.Contains("Alpha")) ? bestLowInk.Value.place : null)
        : null;
      List<string> groupedPlayerSources = GetGroupedSourcesText(player);

      // Single player detailed view --
      // If there's just the one matched player, move the extras to another field.
      if (r.players.Length == 1 && r.teams.Length < 14)
      {
        var fieldBody = otherNames;
        builder.AddField(
          name: fieldHead,
          value: fieldBody.Truncate(DiscordLimits.FIELD_VALUE_LIMIT).Or("(No other names)"),
          inline: false
        );

        var fcsLength = player.FCInformation.Count;
        builder.AddField(
          name: "FCs:",
          value: $"{fcsLength} known friend code".Plural(fcsLength),
          inline: false
        );

        if (currentTeam?.Length > 0)
        {
          builder.AddField(
            name: "Current team:",
            value: currentTeam.Truncate(DiscordLimits.FIELD_VALUE_LIMIT),
            inline: false
          );
        }

        if (oldTeams.Count > 0)
        {
          builder.AddUnrolledList(
            fieldHeader: "Old teams",
            fieldValues: oldTeams
          );
        }

        if (twitch.Length > 0)
        {
          builder.AddUnrolledList(
            fieldHeader: "Twitch",
            fieldValues: twitch
          );
        }

        if (twitter.Length > 0)
        {
          builder.AddUnrolledList(
            fieldHeader: "Twitter",
            fieldValues: twitter
          );
        }

        if (battlefy.Length > 0)
        {
          builder.AddUnrolledList(
            fieldHeader: "Battlefy",
            fieldValues: battlefy
          );
        }

        if (discord.Length > 0)
        {
          builder.AddUnrolledList(
            fieldHeader: "Discord",
            fieldValues: discord
          );
        }

        if (notableResults.Length > 0 || player.PlusMembership.Count > 0 || winningLowInkPos != null)
        {
          var notableResultLines = new List<string>();
          notableResultLines.AddRange(player.PlusMembership.OrderByDescending(plus => plus.Date).Select(plus => $"{PLUS} +{plus.Level} member ({plus.Date:MM yyyy})"));

          if (winningLowInkPos != null)
          {
            if (winningLowInkPos == 1)
            {
              notableResultLines.Add($"{LOW_INK} Low Ink Winner");
            }
            else if (winningLowInkPos == 2)
            {
              notableResultLines.Add($"{LOW_INK} Low Ink 🥈");
            }
            else if (winningLowInkPos == 3)
            {
              notableResultLines.Add($"{LOW_INK} Low Ink 🥉");
            }
          }

          notableResultLines.AddRange(notableResults.Select(result => $"🏆 Won {result}"));

          builder.AddUnrolledList("Notable Wins", notableResultLines);
        }

        if (player.Weapons.Count > 0)
        {
          builder.AddUnrolledList(
            fieldHeader: "Weapons",
            fieldValues: player.Weapons,
            separator: ", "
          );
        }

        // Add Skill/Clout here

        builder.AddUnrolledList("Sources", groupedPlayerSources);
      }

      // Multiple players summary view --
      else
      {
        var emojiNum = AddToReactsDict(reacts, player);
        var additionalInfo = emojiNum != null ? $"\n React {emojiNum} for more…\n" : $"\n More info: /full {player.Id}\n";
        string notableResultsStr = "";
        if (notableResults.Length > 0 || player.PlusMembership.Count > 0 || winningLowInkPos != null)
        {
          var latestPlus = player.PlusMembership.OrderByDescending(plus => plus.Date).FirstOrDefault();
          notableResultsStr += latestPlus == null ? "" : $"{PLUS} +{latestPlus.Level} member ({latestPlus.Date:MMM yyyy})\n";

          if (winningLowInkPos != null)
          {
            if (winningLowInkPos == 1)
            {
              notableResultsStr += ($"{LOW_INK} Low Ink Winner\n");
            }
            else if (winningLowInkPos == 2)
            {
              notableResultsStr += ($"{LOW_INK} Low Ink 🥈\n");
            }
            else if (winningLowInkPos == 3)
            {
              notableResultsStr += ($"{LOW_INK} Low Ink 🥉\n");
            }
          }

          notableResultsStr += string.Join("\n", notableResults.Select(result => $"🏆 Won {result}"));
        }

        int fcsLength = player.FCInformation.Count;
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
        var sourcesField = "Sources:\n" + string.Join("\n", groupedPlayerSources);
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
          name: fieldHead,
          value: fieldBody,
          inline: false
          );
        await Task.Yield();
      }
    }

    private async Task AddMatchedTeam(EmbedBuilder builder, Dictionary<string, IReadonlySourceable> reacts, SlappResponseObject r, Team team)
    {
      var groupedTeamSources = GetGroupedSourcesText(team);
      var players = r.playersForTeams[team.Id];
      var playersInTeam = new List<Player>();
      var playersEverInTeam = new List<Player>();
      var playerStrings = new List<string>();
      var playerStringsDetailed = new List<string>();
      foreach (var playerTuple in players)
      {
        const int NAMES_TO_TAKE = 9;
        var p = playerTuple.Item1;
        var inTeam = playerTuple.Item2;
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
      if (r.teams.Length == 1)
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
        var numPlayersStr = players.Length + " player".Plural(players);
        var info = (divPhrase + tagsStr + numPlayersStr).Or(".");
        builder.AddField(
          name: team.ToString().Truncate(DiscordLimits.FIELD_NAME_LIMIT).Or("(Unnamed Team)"),
          value: info.Truncate(DiscordLimits.FIELD_VALUE_LIMIT),
          inline: false
          );

        // Show team's alts if any
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

        builder.AddField(
          name: "Slapp Id:",
          value: team.Id.ToString().Truncate(DiscordLimits.FIELD_VALUE_LIMIT),
          inline: false
          );

        builder.AddUnrolledList("Sources", groupedTeamSources);
      }
      // Multiple teams summary view --
      else
      {
        var emojiNum = AddToReactsDict(reacts, team);
        var additionalInfo = emojiNum == null ? $"\n React {emojiNum} for more…\n" : $"\n More info: /full {team.Id}\n";
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
          name: team.ToString().Truncate(DiscordLimits.FIELD_NAME_LIMIT).Or("(Unnamed Team)"),
          value: fieldBody.Truncate(DiscordLimits.FIELD_VALUE_LIMIT),
          inline: false
        );
        await Task.Yield();
      }
    }

    /// <summary>
    /// Add the reaction to the reacts buffer to keep track of message reactions.
    /// </summary>
    private void AddToReactsBuffer(ulong messageId, IDictionary<string, IReadonlySourceable> reacts)
    {
      this.slappReactsQueue[messageId] = reacts;
      // Always ensure we have room for the last message.
      if (this.slappReactsQueue.Count > numbersKeyCaps.Count)
      {
        this.slappReactsQueue.Remove(this.slappReactsQueue.First().Key);
      }
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
    private static ImmutableSortedDictionary<string, List<Source>> GetGroupedSources(IReadonlySourceable obj)
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
    private static List<string> GetGroupedSourcesText(IReadonlySourceable obj)
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