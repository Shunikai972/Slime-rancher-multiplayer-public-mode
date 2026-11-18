using System;
using SRMultiplayer.EpicSDK;

namespace SRMultiplayer
{
    /// <summary>
    /// What SlimeBridge's own plugin calls (by reflection, so SRMP never depends on it): host with the
    /// session code the player shares in both games, join with it, and read where the session stands.
    /// Every member is safe to call before EOS has signed in: it then reports why it cannot act yet.
    /// </summary>
    public static class SlimeBridgeLink
    {
        /// <summary>The lobby code while hosting or connected, else an empty string.</summary>
        public static string CurrentCode
        {
            get
            {
                try
                {
                    var lobby = EpicApplication.Instance == null ? null : EpicApplication.Instance.Lobby;
                    return lobby == null || lobby.LobbyId == null ? "" : lobby.LobbyId.ToString();
                }
                catch (Exception) { return ""; }
            }
        }

        public static bool IsHost
        {
            get
            {
                try { var lobby = EpicApplication.Instance == null ? null : EpicApplication.Instance.Lobby; return lobby != null && lobby.IsLobbyOwner && CurrentCode.Length > 0; }
                catch (Exception) { return false; }
            }
        }

        public static bool IsSignedIn
        {
            get
            {
                try { var app = EpicApplication.Instance; return app != null && app.Authentication != null && app.Authentication.IsLoggedIn; }
                catch (Exception) { return false; }
            }
        }

        /// <summary>
        /// Signs in to Epic Online Services when that has not happened yet. SRMP signs in with the player's SRMP username:
        /// a player who never opened SRMP's menu has none, and Epic refuses a sign-in without a display name. Such a player
        /// gets <paramref name="fallbackName"/> (kept as their SRMP username, which they can change in SRMP's menu).
        /// Returns "" when signed in or signing in, else why not. Safe to call every frame: it never starts two sign-ins.
        /// </summary>
        public static string SignIn(string fallbackName)
        {
            try
            {
                var app = EpicApplication.Instance;
                if (app == null || app.Authentication == null) return "srmp_not_ready";
                if (app.Authentication.IsLoggedIn) return "";
                var name = string.IsNullOrEmpty(Globals.Username) ? fallbackName : Globals.Username;
                if (string.IsNullOrEmpty(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_][\w]*$")) name = "Rancher";
                if (string.IsNullOrEmpty(Globals.Username))
                {
                    Globals.Username = name;
                    UnityEngine.PlayerPrefs.SetString("SRMP_Username", name);
                    UnityEngine.PlayerPrefs.Save();
                }
                app.Authentication.Login(name);
                return "";
            }
            catch (Exception ex) { return "sign_in_failed:" + ex.GetType().Name; }
        }

        /// <summary>Opens a lobby; with a valid code (7 or 9 characters) that code is the lobby's. Returns why not, or "".</summary>
        public static string Host(string code)
        {
            try
            {
                var lobby = EpicApplication.Instance == null ? null : EpicApplication.Instance.Lobby;
                if (lobby == null) return "srmp_not_ready";
                if (!IsSignedIn) return "eos_signing_in";
                var normalized = string.IsNullOrEmpty(code) ? null : ServerCode.Normalize(code);
                if (normalized != null && !ServerCode.IsValid(normalized)) return "bad_code";
                if (lobby.IsInLobby) return normalized == null || CurrentCode == normalized ? "" : "already_in_lobby";
                if (Levels.isMainMenu()) return "no_world";
                EpicLobby.RequestedCode = normalized;
                lobby.CreateLobby();
                return "";
            }
            catch (Exception ex) { return "host_failed:" + ex.GetType().Name; }
        }

        /// <summary>Joins the lobby with this code (7 or 9 characters). Returns why not, or "".</summary>
        public static string Join(string code)
        {
            try
            {
                var lobby = EpicApplication.Instance == null ? null : EpicApplication.Instance.Lobby;
                if (lobby == null) return "srmp_not_ready";
                if (!IsSignedIn) return "eos_signing_in";
                var normalized = ServerCode.Normalize(code ?? "");
                if (!ServerCode.IsValid(normalized)) return "bad_code";
                if (lobby.IsInLobby) return CurrentCode == normalized ? "" : "already_in_lobby";
                lobby.JoinLobby(normalized);
                return "";
            }
            catch (Exception ex) { return "join_failed:" + ex.GetType().Name; }
        }
    }
}
