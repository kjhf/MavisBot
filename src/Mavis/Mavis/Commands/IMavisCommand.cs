using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public interface IMavisCommand
  {
    public string Name { get; }

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client);

    public Task Execute(DiscordSocketClient _client, SocketSlashCommand command);
  }
}