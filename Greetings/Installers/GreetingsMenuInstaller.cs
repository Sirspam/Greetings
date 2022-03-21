using Greetings.Configuration;
using Greetings.UI;
using Greetings.UI.ViewControllers;
using Greetings.Utils;
using Zenject;

namespace Greetings.Installers
{
    internal class GreetingsMenuInstaller : Installer
    {
        private readonly PluginConfig _pluginConfig;

        public GreetingsMenuInstaller(PluginConfig pluginConfig)
        {
            _pluginConfig = pluginConfig;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(_pluginConfig).AsSingle();
            Container.Bind<UIUtils>().AsSingle();
            Container.Bind<ScreenUtils>().AsSingle();
            Container.BindInterfacesAndSelfTo<CheeseUtils>().AsSingle();
            
            Container.BindInterfacesTo<GreetingsController>().AsSingle();
            Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
            Container.Bind<FloorTextViewController>().FromNewComponentAsViewController().AsSingle();

            Container.Bind<GreetingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            Container.Bind<GreetingsSettingsViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ScreenControlsViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<VideoSelectionViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<DeleteConfirmationViewController>().AsSingle();
        }
    }
}