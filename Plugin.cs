using System;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine.SceneManagement;
using DiscordRPC;
using Lachee.Discord.Control;

namespace MiSideRichPresence
{
    public static class Info
    {
        public const string PLUGIN_GUID = "etakat.MiSideRichPresence";
        public const string PLUGIN_NAME = "MiSideRichPresence";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string APPLICATION_ID = "1320457436032794685";
    }

    [BepInPlugin(Info.PLUGIN_GUID, Info.PLUGIN_NAME, Info.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        private new static ManualLogSource Log;
        private static DiscordRpcClient _client;

        private readonly RichPresence _presence = new RichPresence();

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("TODO<etakat>: This code is ass. Session terminated.");

            InitializeDiscordClient();

            SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode>)OnSceneLoaded;
        }

        public override bool Unload()
        {
            SceneManager.sceneLoaded -= (UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode>)OnSceneLoaded;
            DisposeDiscordClient();
            return true;
        }

        private void InitializeDiscordClient()
        {
            try
            {
                var unityPipe = new UnityNamedPipe();
                _client = new DiscordRpcClient(
                    Info.APPLICATION_ID,
                    pipe: -1,
                    autoEvents: false,
                    client: unityPipe
                );

                _client.OnError += (sender, e) =>
                {
                    Log.LogError($"Discord RPC Error: {e.Message}");
                    Thread.Sleep(2500);
                };

                _client.OnClose += (sender, e) =>
                {
                    Thread.Sleep(2500);
                };

                _client.Logger = new DiscordRPC.Logging.ConsoleLogger
                {
                    Level = DiscordRPC.Logging.LogLevel.Warning
                };

                _client.Initialize();
                
                if(!_presence.HasTimestamps())
                {
                    _presence.Timestamps = Timestamps.Now;
                }
            }
            catch (Exception ex)
            {
                _client = null;
                Log.LogError($"Failed to initialize Discord RPC: {ex}");
                Unload();
            }
        }

        private void DisposeDiscordClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            string sceneName = scene.name;
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            UpdateRichPresence(MiSideRichPresence.Scene.GetSceneByName(sceneName));
        }

        private void UpdateRichPresence(Scene scene)
        {
            _presence.Details = scene.DisplayName;

            _presence.Assets = new Assets
            {
                LargeImageKey = scene.IconId,
                LargeImageText = scene.DisplayName,
                SmallImageText = "MiSide"
            };
            
            if (scene != Scene.Loading && scene != Scene.Aihasto && scene != Scene.MainMenu && scene != Scene.Unknown)
            {
                _presence.Assets.SmallImageKey = Scene.DefaultIcon;
            }
            else
            {
                _presence.Assets.SmallImageKey = null;
            }
            
            _client?.SetPresence(_presence);
        }
    }
}
