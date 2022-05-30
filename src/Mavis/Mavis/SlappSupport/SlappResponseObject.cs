using NLog;
using NLog.Fluent;
using SplatTagCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.SlappSupport
{
  internal record class SlappResponseObject
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    public readonly Player[] players;
    private readonly Dictionary<Guid, Team> teamsDict;
    public IReadOnlyCollection<Team> Teams => teamsDict.Values;
    private readonly ITeamResolver splatTagController;
    public readonly Dictionary<Guid, Dictionary<Source, Bracket[]>> placementsForPlayers = new();

    public bool HasPlayers => players.Length > 0;
    public bool HasPlayersPl => players.Length > 1;
    public bool HasTeams => Teams.Count > 0;
    public bool HasTeamsPl => Teams.Count > 1;

    public SlappResponseObject(Player[] players, Team[] teams, ITeamResolver splatTagController)
    {
      this.players = players;
      this.teamsDict = teams.ToDictionary(t => t.Id, t => t);
      this.splatTagController = splatTagController;

      try
      {
        foreach (var player in players)
        {
          placementsForPlayers[player.Id] = new Dictionary<Source, Bracket[]>();
          foreach (var source in player.Sources)
          {
            placementsForPlayers[player.Id][source] = source.Brackets;
          }
        }
      }
      catch (OutOfMemoryException oom)
      {
        const string message = "ERROR: OutOfMemoryException on PlacementsForPlayers. Will continue anyway.";
        log.Error(oom, message);
        placementsForPlayers = new Dictionary<Guid, Dictionary<Source, Bracket[]>>();
      }
    }

    public Team? GetTeam(Guid teamId)
    {
      // First, check our dictionary
      if (teamsDict.TryGetValue(teamId, out var team)) { return team; }

      // If we don't have this entry, search the wider list
      return (this.splatTagController.GetTeamById(teamId, out team)) ? team : null;
    }

    public (Team, ReadOnlyCollection<Source>)[] GetTeamsForPlayer(Player p)
      => p.TeamInformation?.GetAllTeamsOrdered()
          .Select(pair => (GetTeam(pair.Key), pair.Value))
          .Where(pair => pair.Item1 is not null).Select(pair => (pair.Item1!, pair.Value))
          .ToArray()
      ?? Array.Empty<(Team, ReadOnlyCollection<Source>)>();

    /// <summary>
    /// Get a yielded enumerable of placements for this player where the player has come in the given <paramref name="place"/> (1 by default for 1st).
    /// </summary>
    /// <param name="p">The player</param>
    /// <param name="place">The requested place</param>
    /// <returns>A yielded enumerable of placements in form the Source tournament, its bracket, and the Team that the player played for to achieve this result.</returns>
    public IEnumerable<(Source s, Bracket b, Team? t, int place)> GetPlacementsByPlace(Player p, int place = 1)
      => GetPlacements(p, targetPlace: place);

    public IEnumerable<(Source s, Bracket b, Team? t, int place)> GetLowInkPlacements(Player p)
      => GetPlacements(p, targetTourneyNames: new[] { "low-ink-" }, targetBracketNames: new[] { "Alpha", "Beta", "Gamma", "Top Cut" });

    public (Source s, Bracket b, Team? t, int place)? GetBestLowInkPlacement(Player p)
    {
      (Source s, Bracket b, Team? t, int place)? currentBest = null;

      foreach (var lowInkPlacement in GetLowInkPlacements(p))
      {
        if (currentBest == null)
        {
          currentBest = lowInkPlacement;
        }
        else
        {
          var isSameComparison =
            currentBest.Value.b.Name == lowInkPlacement.b.Name
            || (currentBest.Value.b.Name.Contains("Alpha") && lowInkPlacement.b.Name.Contains("Top Cut"))
            || (currentBest.Value.b.Name.Contains("Top Cut") && lowInkPlacement.b.Name.Contains("Alpha"));

          // If lower place (i.e. did better)
          if (isSameComparison)
          {
            if (lowInkPlacement.place < currentBest.Value.place)
            {
              currentBest = lowInkPlacement;
            }
          }
          else
          {
            if (lowInkPlacement.b.Name.Contains("Alpha") || lowInkPlacement.b.Name.Contains("Top Cut"))
            {
              // Better than the current best's bracket
              currentBest = lowInkPlacement;
            }
            else if (lowInkPlacement.b.Name.Contains("Beta") && !currentBest.Value.b.Name.Contains("Alpha") && !currentBest.Value.b.Name.Contains("Top Cut"))
            {
              // Better than the current best's bracket (gamma/unknown)
              currentBest = lowInkPlacement;
            }
            // else Gamma and we don't care / already tested the place.
          }
        }
      }
      return currentBest;
    }

    /// <summary>
    /// Get a yielded enumerable of placements for this player for the given tournament and brackets.
    /// </summary>
    /// <param name="p">The player</param>
    /// <param name="targetTourneyNames">The requested tournaments (null for no filter)</param>
    /// <param name="targetBracketNames">The requested brackets (null for no filter)</param>
    /// <param name="targetPlace">The requested place (null for no filter). e.g. 1 is 1st place.</param>
    /// <returns>A yielded enumerable of placements in form the Source tournament, its bracket, and the Team that the player played for to achieve this result.</returns>
    public IEnumerable<(Source s, Bracket b, Team? t, int place)> GetPlacements(
      Player p,
      ICollection<string>? targetTourneyNames = null,
      ICollection<string>? targetBracketNames = null,
      int? targetPlace = null
      )
    {
      var result = new List<(Source s, Bracket b, Team? t, int place)>();
      var placementsForPlayer = this.placementsForPlayers[p.Id];
      var filteredSources = placementsForPlayer.Where(_ => true);

      if (targetTourneyNames != null)
      {
        filteredSources = filteredSources.Where(pair => targetTourneyNames.Any(n => pair.Key.Name.Contains(n)));
      }

      foreach (var (source, brackets) in filteredSources)
      {
        var filteredBrackets = brackets.Where(b => b.Placements.HasPlacements);
        if (targetBracketNames != null)
        {
          filteredBrackets = filteredBrackets.Where(b => targetBracketNames.Any(n => b.Name.Contains(n)));
        }

        foreach (Bracket bracket in filteredBrackets)
        {
          var filteredPlacements = bracket.Placements.PlayersByPlacement.Where(tup => tup.Value.Contains(p.Id));
          if (targetPlace != null)
          {
            filteredPlacements = filteredPlacements.Where(tup => tup.Key == targetPlace);
          }

          foreach (var (place, _) in filteredPlacements)
          {
            result.Add((source, bracket, ResolveTeamForPlacement(source, bracket, p, place), place));
          }
        }
      }
      return result;
    }

    private Team? ResolveTeamForPlacement(Source s, Bracket b, Player p, int place)
    {
      var teams = b.Placements.TeamsByPlacement.ContainsKey(place) ? b.Placements.TeamsByPlacement[place].ToArray() : null;

      // For first place, we can do a FirstOrDefault here because only one team can come in first.
      if (teams?.Length > 1)
      {
        // But for tied-place requests, "teams" may have more than one team entry.
        // There are cases where a player has played for multiple teams in the tournament (e.g. an ex-team and current team)
        // and if these teams got the same result, then we need to filter out teams that the player is not associated with for this tourney (this is the Source check).
        // This does not cover the case where the player has played for multiple teams in this tournament (e.g. is a sub),
        // AND the teams have a tied final place, as we ultimately would end up with both teams in this result.
        if (p.TeamInformation != null)
        {
          var playerTeams =
            p.TeamInformation.GetTeamsSourcedUnordered()
            .Where(pair => pair.Value.Contains(s))
            .Where(playerTeamPair => teams.Contains(playerTeamPair.Key));
          return playerTeams?.Any() == true ? GetTeam(playerTeams.First().Key) : null;
        }
      }
      // else
      return (teams == null || teams.Length < 1) ? null : GetTeam(teams[0]);
    }
  }
}