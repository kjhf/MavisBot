using Discord;
using Discord.WebSocket;
using Mavis.Imaging;
using Mavis.Utils;
using NLog;
using System;
using System.DrawingCore;
using System.Linq;
using System.Threading.Tasks;
using DiscordColor = Discord.Color;
using DrawingColor = System.DrawingCore.Color;

namespace Mavis.Commands
{
  internal class ColourMeCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "colour-me";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Give the requesting user a coloured role.")
          .AddOption(new SlashCommandOptionBuilder()
            .WithName("arg").WithType(ApplicationCommandOptionType.String)
            .WithDescription("The _colour_ (or `random` or `remove`). Colour may be in English or 3- or 6- digit hex code.").WithRequired(true)
            .AddChoice("random", "random").AddChoice("remove", "remove")
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

      SocketGuild? guild = command.GetGuild();
      if (guild == null)
      {
        const string message = "❌ I can't find the guild. Are we in DMs? 😅";
        await command.RespondAsync(message, ephemeral: true).ConfigureAwait(false);
        return;
      }

      await guild.DownloadUsersAsync();
      var user = command.GetGuildUser();
      if (user == null)
      {
        const string message = "❌ I can't get the guild instance for your account. Are we in DMs? 😅";
        await command.RespondAsync(message, ephemeral: true).ConfigureAwait(false);
        return;
      }

      // If the user specified remove, do that.
      if (input.Equals("remove", StringComparison.InvariantCultureIgnoreCase))
      {
        await RemoveColourRoles(user).ConfigureAwait(false);
        await CleanGuildColourRoles(guild).ConfigureAwait(false);
        return;
      }

      // Otherwise find the colour and add the role appropriately
      var inputColour = ImageManipulator.FromString(input);
      if (inputColour == null)
      {
        const string message = "❌ I don't understand your colour.";
        await command.RespondAsync(message, ephemeral: true).ConfigureAwait(false);
      }
      else
      {
        DiscordColor discordColor = inputColour.Value.ToDiscordColor();
        await RemoveColourRoles(user).ConfigureAwait(false);
        await CleanGuildColourRoles(guild).ConfigureAwait(false);

        // Add the requested role to the user
        var requestedRoleName = $"mavis_{inputColour.Value.ToArgb()}";
        var requestedRole = guild.Roles.FirstOrDefault(r => r.Name.TrimStart('@') == requestedRoleName);
        if (requestedRole == null)
        {
          log.Trace($"🔵 Creating role {requestedRoleName} and adding to {user.Id}.");
          try
          {
            var restRole = await guild.CreateRoleAsync(requestedRoleName, null, discordColor, options: new RequestOptions { AuditLogReason = $"Mavis (Creating role requested by {user.Id})" }).ConfigureAwait(false);
            if (restRole == null)
            {
              throw new ArgumentNullException(nameof(restRole), "Null response from guild");
            }
            await user.AddRoleAsync(restRole, new RequestOptions { AuditLogReason = $"Mavis (Adding colour role requested by {user.Id})" }).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            string message = "❌ I couldn't create the role. Did you give me a bad value? " + ex.Message;
            await command.RespondAsync(message, ephemeral: true).ConfigureAwait(false);
            return;
          }
        }
        else
        {
          log.Trace($"🔵 Adding pre-existing role {requestedRoleName} to {user.Id}.");
          await user.AddRoleAsync(requestedRole, new RequestOptions { AuditLogReason = $"Mavis (Adding colour role requested by {user.Id})" }).ConfigureAwait(false);
        }
      }
    }

    private static async Task CleanGuildColourRoles(SocketGuild guild)
    {
      // Remove any roles from the guild that no longer have any users with it
      var rolesToRemove = guild.Roles.Where(r => !r.Members.Any() && (r.Name.StartsWith("dola_") || r.Name.StartsWith("mavis_"))).ToList();
      if (rolesToRemove.Count > 0)
      {
        var rolesToRemoveNamesString = string.Join(", ", rolesToRemove.Select(r => r.Name));
        log.Trace($"🔴 Removing {rolesToRemoveNamesString} from {guild.Name}.");
        await guild.RemoveRolesAsync(rolesToRemove, new RequestOptions { AuditLogReason = "Mavis (No more users with this role)" }).ConfigureAwait(false);
      }
    }

    private static async Task RemoveColourRoles(SocketGuildUser user)
    {
      // If the user has any colour roles already, remove them.
      var colourRoles = user.Roles.Where(r => r.Name.StartsWith("dola_") || r.Name.StartsWith("mavis_"));
      if (colourRoles.Any())
      {
        var colourRoleNamesString = string.Join(", ", colourRoles.Select(r => r.Name));
        log.Trace($"🔴 Removing {colourRoleNamesString} from {user.Username}.");
        await user.RemoveRolesAsync(colourRoles, new RequestOptions { AuditLogReason = $"Mavis (Removing role requested by {user.Id})" }).ConfigureAwait(false);
      }
    }
  }
}