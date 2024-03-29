﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public static class Constants
    {
        public readonly static bool IsDebug = false;
        public readonly static string LoadingSceneName = "LoadingScene";
        public readonly static string MainSceneName = "MainScene";
        public readonly static string GameSceneName = "GameScene";

        public readonly static float WaitForSecondsAfterEachLoadAsset = 0.450f;

        public const string VSyncSettingKey = "vsync";
        public const string BiggerTextSettingKey = "biggertext";
        public const string VolumeSettingKey = "volume";
        public const string MixerMasterVolumeKey = "mastervol";
        public const int VSyncEnabledValue = 1;
        public const int BiggerGameTextValue = 1;
        public const int NotBiggerGameTextValue = 0;
        public const float DefaultVolume = 0.35f;
        public const float MaxVolume = 1f;

        public const int NormalGameTextSize = 18;
        public const int BigGameTextSize = 26;

        readonly static string BaseUrl = "https://api.taletactics.com";
        //readonly static string BaseUrl = "https://localhost:7216";
        // For android to work, you have to disable HTTPS redirection in API and hardcode this `var url = $"http://10.0.2.2:5216{pathBase}/{subPath}";`
        // in `public abstract class ModelEntityHandler`
        //readonly static string BaseUrl = "http://10.0.2.2:5216";
        
        public readonly static string HubUrl = $"{BaseUrl}/game-hub";

        public const int HubTimeoutSeconds = 300;
        public const int HubStopTimeoutSeconds = 150;

        public const float AudioFadeInTime = 3f;
        public const float AudioFadeOutTime = 3f;
        public const float ImageFadeInTime = 4f;
        public const float ImageFadeOutTime = 4f;

        public static string GetGameConfigurationUrl(string gameCode)
        {
            return $"{BaseUrl}/games/{gameCode}/configuration";
        }
    }
}
