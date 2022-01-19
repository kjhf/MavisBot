using Discord;
using Discord.WebSocket;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class ZalgoCommand : IMavisCommand
  {
    public static readonly IReadOnlyList<char> ZalgoDown = new char[]
    {
      '\u0316',   '\u0317',   '\u0318',   '\u0319',
      '\u031c',   '\u031d',   '\u031e',   '\u031f',
      '\u0320',   '\u0324',   '\u0325',   '\u0326',
      '\u0329',   '\u032a',   '\u032b',   '\u032c',
      '\u032d',   '\u032e',   '\u032f',   '\u0330',
      '\u0331',   '\u0332',   '\u0333',   '\u0339',
      '\u033a',   '\u033b',   '\u033c',   '\u0345',
      '\u0347',   '\u0348',   '\u0349',   '\u034d',
      '\u034e',   '\u0353',   '\u0354',   '\u0355',
      '\u0356',   '\u0359',   '\u035a',   '\u0323'
    };

    public static readonly IReadOnlyList<char> ZalgoMid = new char[]
    {
      '\u0315',   '\u031b',   '\u0340',   '\u0341',
      '\u0358',   '\u0321',   '\u0322',   '\u0327',
      '\u0328',   '\u0334',   '\u0335',   '\u0336',
      '\u034f',   '\u035c',   '\u035d',   '\u035e',
      '\u035f',   '\u0360',   '\u0362',   '\u0338',
      '\u0337',   '\u0361',   '\u0489'
    };

    public static readonly IReadOnlyList<char> ZalgoUp = new char[]
    {
      '\u030d',   '\u030e',   '\u0304',   '\u0305',
      '\u033f',   '\u0311',   '\u0306',   '\u0310',
      '\u0352',   '\u0357',   '\u0351',   '\u0307',
      '\u0308',   '\u030a',   '\u0342',   '\u0343',
      '\u0344',   '\u034a',   '\u034b',   '\u034c',
      '\u0303',   '\u0302',   '\u030c',   '\u0350',
      '\u0300',   '\u0301',   '\u030b',   '\u030f',
      '\u0312',   '\u0313',   '\u0314',   '\u033d',
      '\u0309',   '\u0363',   '\u0364',   '\u0365',
      '\u0366',   '\u0367',   '\u0368',   '\u0369',
      '\u036a',   '\u036b',   '\u036c',   '\u036d',
      '\u036e',   '\u036f',   '\u033e',   '\u035b',
    };

    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    public string Name => "zalgo";

    public static string AddZalgo(string input)
    {
      var rand = new Random();
      StringBuilder sb = new();
      foreach (char c in input)
      {
        switch (rand.Next() % 5)
        {
          case 0: sb.Append(c); sb.Append(ZalgoUp[rand.Next() % ZalgoUp.Count]); break;
          case 1: sb.Append(c); sb.Append(ZalgoMid[rand.Next() % ZalgoMid.Count]); break;
          case 2: sb.Append(c); sb.Append(ZalgoDown[rand.Next() % ZalgoDown.Count]); break;
          default: sb.Append(c); break;
        }
      }
      return sb.ToString();
    }

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Hẽ ͕c͞oͥm̡eṡ.")
          .AddOption("text", ApplicationCommandOptionType.String, "Text to transform", isRequired: true)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      log.Trace($"Processing {Name}.");

      string? detail = command.Data.Options.FirstOrDefault()?.Value?.ToString();
      if (detail == null)
      {
        detail = "Zalgo";
      }
      await command.RespondAsync(AddZalgo(detail));
    }
  }
}