using SplatTagCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.SlappSupport
{
  internal record class SlappResponseObject
  {
    public readonly Player[] players;
    public readonly Team[] teams;
    public readonly HashSet<Source> sources = new();
    public readonly Dictionary<Guid, Team> additionalTeams = new();
    public readonly Dictionary<Guid, (Player, bool)[]> playersForTeams = new();
    public readonly Dictionary<Guid, Dictionary<string, Bracket[]>> placementsForPlayers = new();

    public bool HasPlayers => players.Length > 0;
    public bool HasPlayersPl => players.Length > 1;
    public bool HasTeams => teams.Length > 0;
    public bool HasTeamsPl => teams.Length > 1;

    public SlappResponseObject(Player[] players, Team[] teams, SplatTagController splatTagController)
    {
      this.players = players;
      this.teams = teams;

      if (HasPlayers || HasTeams)
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

        sources = new HashSet<Source>();
        foreach (var s in players.SelectMany(p => p.Sources))
        {
          sources.Add(s);
        }
        foreach (var s in teams.SelectMany(t => t.Sources))
        {
          sources.Add(s);
        }
        foreach (var s in additionalTeams.Values.SelectMany(t => t.Sources))
        {
          sources.Add(s);
        }
        foreach (var s in playersForTeams.Values.SelectMany(tupleArray => tupleArray.SelectMany(p => p.Item1.Sources)))
        {
          sources.Add(s);
        }
        sources.Add(Builtins.BuiltinSource);
        sources.Add(Builtins.ManualSource);

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
    }
  }
}