using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static dResetScore.dResetScore;

namespace dResetScore
{
    public static class PlayerUtils
    {
        public static void PrintToChat(CCSPlayerController player, string msg, bool isError)
        {
            string checkIcon = isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}✔{ChatColors.Lime}";
            player.PrintToChat($" {ChatColors.DarkRed}► {ChatColors.Green}[{ChatColors.DarkRed} RESETSCORE {ChatColors.Green}] {checkIcon} {msg}");
        }

        public static bool CanUseResetScore(CCSPlayerController player)
        {
            int cooldown = Config.config.Settings.ResetScoreCooldown;

            if (cooldown == 0) return true;

            if (lastResetScoreUsage.TryGetValue(player.SteamID, out DateTime lastUsedTime))
            {
                return (DateTime.Now - lastUsedTime).TotalSeconds >= cooldown;
            }

            return true;
        }

        public static int GetRemainingCooldown(CCSPlayerController player)
        {
            int cooldown = Config.config.Settings.ResetScoreCooldown;

            if (lastResetScoreUsage.TryGetValue(player.SteamID, out DateTime lastUsedTime))
            {
                int remainingTime = cooldown - (int)(DateTime.Now - lastUsedTime).TotalSeconds;
                return remainingTime > 0 ? remainingTime : 0;
            }

            return 0;
        }

        public static void SetScore(CCSPlayerController? player, int kills, int deaths, int assists, int damage, int mvps, int score)
        {
            if (player == null) return;

            var matchStats = player.ActionTrackingServices?.MatchStats;
            if (matchStats != null)
            {
                matchStats.Kills = kills;
                matchStats.Deaths = deaths;
                matchStats.Assists = assists;
                matchStats.Damage = damage;
            }
            
            player.Score = score;
            player.MVPs = mvps;

            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pActionTrackingServices");
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMVPs");
        }
    }
}