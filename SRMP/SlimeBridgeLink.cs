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
        /// Starts SRMP's own sign-in (anonymous device sign-in, no account) with the name the player chose: the name given
        /// here (typed in SlimeBridge), else the one already saved in SRMP's menu. Never invents a name: without one it
        /// returns "needs_name" and does nothing. Returns "" when signed in or signing in.
        /// </summary>
        public static string SignIn(string name)
        {
            try
            {
                var app = EpicApplication.Instance;
                if (app == null || app.Authentication == null) return "srmp_not_ready";
                if (app.Authentication.IsLoggedIn) return "";
                var valid = !string.IsNullOrEmpty(name) && System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_][\w]*$");
                if (!string.IsNullOrEmpty(name) && !valid) return "bad_name";
                if (valid)
                {
                    Globals.Username = name;
                    UnityEngine.PlayerPrefs.SetString("SRMP_Username", name);
                    UnityEngine.PlayerPrefs.Save();
                }
                if (string.IsNullOrEmpty(Globals.Username)) return "needs_name";
                app.Authentication.Login(Globals.Username);
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
