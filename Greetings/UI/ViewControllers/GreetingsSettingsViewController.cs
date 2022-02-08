﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Greetings.Configuration;
using Zenject;

namespace Greetings.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\GreetingsSettingsView")]
    [ViewDefinition("Greetings.UI.Views.GreetingsSettingsView.bsml")]
    internal class GreetingsSettingsViewController : BSMLAutomaticViewController
    {
        private PluginConfig _pluginConfig = null!;

        [UIValue("use-random-video")]
        private bool UseRandomVideo
        {
            get => _pluginConfig.RandomVideo;
            set => _pluginConfig.RandomVideo = value;
        }

        [UIValue("await-fps")]
        private bool AwaitFps
        {
            get => _pluginConfig.AwaitFps;
            set => _pluginConfig.AwaitFps = value;
        }

        [UIValue("await-hmd")]
        private bool AwaitHmd
        {
            get => _pluginConfig.AwaitHmd;
            set => _pluginConfig.AwaitHmd = value;
        }

        [UIValue("target-fps")]
        private int TargetFps
        {
            get => _pluginConfig.TargetFps;
            set => _pluginConfig.TargetFps = value;
        }
        
        [UIValue("fps-streak")]
        private int FpsStreak
        {
            get => _pluginConfig.FpsStreak;
            set => _pluginConfig.FpsStreak = value;
        }
        
        [UIValue("max-wait-time")]
        private int MaxWaitTime
        {
            get => _pluginConfig.MaxWaitTime;
            set => _pluginConfig.MaxWaitTime = value;
        }

        [Inject]
        public void Construct(PluginConfig pluginConfig)
        {
            _pluginConfig = pluginConfig;
        }
    }
}