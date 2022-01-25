using BeatSaberMarkupLanguage.Attributes;
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

        [Inject]
        public void Construct(PluginConfig pluginConfig)
        {
            _pluginConfig = pluginConfig;
        }
    }
}