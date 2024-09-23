using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static dResetScore.dResetScore;

namespace dResetScore
{
    public static class Command
    {
        public static void Load()
        {
            var config = Config.LoadedConfig;
            if (config == null || config.Commands == null) return;

            var commands = new Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)>
            {
                { SplitCommands(config.Commands.ResetScore_Commands), ("Reset personal Score", Command_ResetScore) },
                { SplitCommands(config.Commands.ResetTeamScore_Commands), ("Reset Score for a Team", Command_ResetTeamScore) },
                { SplitCommands(config.Commands.SetScore_Commands), ("Set score for a Player", Command_SetScore) },
                { SplitCommands(config.Commands.SetTeamScore_Commands), ("Set score for a Team", Command_SetTeamScore) }
            };

            foreach (var commandPair in commands)
            {
                foreach (var command in commandPair.Key)
                {
                    Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
                }
            }
        }

        private static IEnumerable<string> SplitCommands(string commands)
        {
            return commands.Split(',').Select(c => c.Trim());
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public static void Command_ResetScore(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            string resetScoreFlag = Config.config.Settings.ResetScoreFlag;
            int resetScorePayment = Config.config.Settings.ResetScorePayment;
            if (!PlayerUtils.CanUseResetScore(player))
            {
                int remainingCooldown = PlayerUtils.GetRemainingCooldown(player);
                player.PrintToChat($" {ChatColors.Green}――――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――――");
                PlayerUtils.PrintToChat(player, $"Błąd. Możesz ponownie użyć tej komendy za {ChatColors.DarkRed}{remainingCooldown} {ChatColors.LightRed}sekund.", true);
                player.PrintToChat($" {ChatColors.Green}――――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――――");
                return;
            }

            if (!string.IsNullOrEmpty(resetScoreFlag))
            {
                var requiredFlags = resetScoreFlag.Split(',').Select(flag => flag.Trim()).ToArray();
                bool hasPermission = requiredFlags.Any(flag => AdminManager.PlayerHasPermissions(player, flag));

                if (!hasPermission)
                {
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    PlayerUtils.PrintToChat(player, $"Błąd. Nie posiadasz dostępu do tej komendy.", true);
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    return;
                }
            }

            var inGameMoneyServices = player.InGameMoneyServices;
            if (inGameMoneyServices == null || inGameMoneyServices.Account < resetScorePayment)
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Musisz mieć przynajmniej {ChatColors.DarkRed}{resetScorePayment}$ {ChatColors.Lime}, aby zresetować statystyki.", true);
                return;
            }

            inGameMoneyServices.Account -= resetScorePayment;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

            PlayerUtils.SetScore(player, 0, 0, 0, 0, 0, 0);
            player.PrintToChat($" {ChatColors.Green}―――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――――――");
            PlayerUtils.PrintToChat(player, $"Twoje wszystkie statystyki zostały {ChatColors.DarkRed}Zresetowane{ChatColors.Lime}.", false);
            player.PrintToChat($" {ChatColors.Green}―――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――――――");

            if (Config.config.Settings.ResetScoreCooldown > 0)
            {
                lastResetScoreUsage[player.SteamID] = DateTime.Now;
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public static void Command_ResetTeamScore(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            string adminFlag = Config.config.Settings.AdminFlag;
            if (!string.IsNullOrEmpty(adminFlag))
            {
                var requiredFlags = adminFlag.Split(',').Select(flag => flag.Trim()).ToArray();
                bool hasPermission = requiredFlags.Any(flag => AdminManager.PlayerHasPermissions(player, flag));

                if (!hasPermission)
                {
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    PlayerUtils.PrintToChat(player, $"Błąd. Nie posiadasz dostępu do tej komendy.", true);
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    return;
                }
            }

            if (command.ArgCount == 0)
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Poprawne użycie: {ChatColors.DarkRed}!resetteamscore <ct/t>", true);
                return;
            }

            string teamArg = command.ArgByIndex(1).ToLower();
            CsTeam teamToReset;
            
            if (teamArg == "ct")
            {
                teamToReset = CsTeam.CounterTerrorist;
            }
            else if (teamArg == "t")
            {
                teamToReset = CsTeam.Terrorist;
            }
            else
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Poprawne użycie: {ChatColors.DarkRed}!resetteamscore <ct/t>", true);
                return;
            }

            var teamPlayers = Utilities.GetPlayers().Where(p => p.Team == teamToReset).ToList();
            if (!teamPlayers.Any())
            {
                PlayerUtils.PrintToChat(player, $"Błąd. W drużynie {ChatColors.DarkRed}{(teamToReset == CsTeam.CounterTerrorist ? "CT" : "T")}{ChatColors.LightRed} nie znaleziono żadnych graczy.", true);
                return;
            }

            foreach (var playerReset in teamPlayers)
            {
                PlayerUtils.SetScore(playerReset, 0, 0, 0, 0, 0, 0);
                PlayerUtils.PrintToChat(playerReset, $"Twoje statystyki zostały zresetowane przez admina {ChatColors.DarkRed}{player.PlayerName}{ChatColors.Lime}.", false);
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public static void Command_SetScore(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;
            
            string adminFlag = Config.config.Settings.AdminFlag;
            if (!string.IsNullOrEmpty(adminFlag))
            {
                var requiredFlags = adminFlag.Split(',').Select(flag => flag.Trim()).ToArray();
                bool hasPermission = requiredFlags.Any(flag => AdminManager.PlayerHasPermissions(player, flag));

                if (!hasPermission)
                {
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    PlayerUtils.PrintToChat(player, $"Błąd. Nie posiadasz dostępu do tej komendy.", true);
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    return;
                }
            }

            if (command.ArgCount < 8)
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Użycie: {ChatColors.DarkRed}!setscore <target> <kills> <deaths> <assists> <damage> <mvps> <score>", true);
                return;
            }

            string targetName = command.ArgByIndex(1);
            var targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => p.PlayerName.Contains(targetName, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer == null)
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Nie znaleziono gracza o nazwie {ChatColors.DarkRed}{targetName}{ChatColors.LightRed}.", true);
                return;
            }

            if (!int.TryParse(command.ArgByIndex(2), out int kills) || !int.TryParse(command.ArgByIndex(3), out int deaths) || !int.TryParse(command.ArgByIndex(4), out int assists) || !int.TryParse(command.ArgByIndex(5), out int damage) || !int.TryParse(command.ArgByIndex(6), out int mvps) || !int.TryParse(command.ArgByIndex(7), out int score))
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Wszystkie wartości ({ChatColors.DarkRed}kills, deaths, assists, damage, mvps, score{ChatColors.LightRed}) muszą być liczbami całkowitymi.", true);
                return;
            }

            PlayerUtils.SetScore(targetPlayer, kills, deaths, assists, damage, mvps, score);
            PlayerUtils.PrintToChat(targetPlayer, $"Twoje statystyki zostały zmienione przez admina {ChatColors.DarkRed}{player.PlayerName}{ChatColors.Lime}.", false);
            PlayerUtils.PrintToChat(player, $"Ustawiłeś statystyki graczowi {ChatColors.DarkRed}{targetPlayer.PlayerName}{ChatColors.Lime}.", false);
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public static void Command_SetTeamScore(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            string adminFlag = Config.config.Settings.AdminFlag;
            if (!string.IsNullOrEmpty(adminFlag))
            {
                var requiredFlags = adminFlag.Split(',').Select(flag => flag.Trim()).ToArray();
                bool hasPermission = requiredFlags.Any(flag => AdminManager.PlayerHasPermissions(player, flag));

                if (!hasPermission)
                {
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    PlayerUtils.PrintToChat(player, $"Błąd. Nie posiadasz dostępu do tej komendy.", true);
                    player.PrintToChat($" {ChatColors.Green}――――――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}――――――――――――――");
                    return;
                }
            }

            if (command.ArgCount < 8)
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Użycie: {ChatColors.DarkRed}!setteamscore <ct/t> <kills> <deaths> <assists> <damage> <mvps> <score>", true);
                return;
            }

            string teamArg = command.ArgByIndex(1).ToLower();
            CsTeam teamToSet;

            if (teamArg == "ct")
            {
                teamToSet = CsTeam.CounterTerrorist;
            }
            else if (teamArg == "t")
            {
                teamToSet = CsTeam.Terrorist;
            }
            else
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Użycie: {ChatColors.DarkRed}!setteamscore <ct/t> <kills> <deaths> <assists> <damage> <mvps> <score>", true);
                return;
            }

            if (!int.TryParse(command.ArgByIndex(2), out int kills) || !int.TryParse(command.ArgByIndex(3), out int deaths) || !int.TryParse(command.ArgByIndex(4), out int assists) || !int.TryParse(command.ArgByIndex(5), out int damage) || !int.TryParse(command.ArgByIndex(6), out int mvps) || !int.TryParse(command.ArgByIndex(7), out int score))
            {
                PlayerUtils.PrintToChat(player, $"Błąd. Wszystkie wartości ({ChatColors.DarkRed}kills, deaths, assists, damage, mvps, score{ChatColors.LightRed}) muszą być liczbami całkowitymi.", true);
                return;
            }

            var teamPlayers = Utilities.GetPlayers().Where(p => p.Team == teamToSet).ToList();
            if (!teamPlayers.Any())
            {
                PlayerUtils.PrintToChat(player, $"Błąd. W drużynie {ChatColors.DarkRed}{(teamToSet == CsTeam.CounterTerrorist ? "CT" : "T")}{ChatColors.LightRed} nie znaleziono żadnych graczy.", true);
                return;
            }

            foreach (var playerSet in teamPlayers)
            {
                PlayerUtils.SetScore(playerSet, kills, deaths, assists, damage, mvps, score);
                PlayerUtils.PrintToChat(playerSet, $"Twoje statystyki zostały zmienione przez admina {ChatColors.DarkRed}{player.PlayerName}{ChatColors.Lime}.", false);
            }

            PlayerUtils.PrintToChat(player, $"Ustawiłeś statystyki dla wszystkich graczy z drużyny {ChatColors.DarkRed}{teamArg.ToUpper()} {ChatColors.Lime}({ChatColors.DarkRed}{teamPlayers.Count()} graczy{ChatColors.Lime}).", false);
        }
    }
}