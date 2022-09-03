using Greetings.Configuration;
using Greetings.Managers;
using Greetings.UI.FlowCoordinator;
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
			Container.Bind<GreetingsUtils>().AsSingle();

			Container.BindInterfacesAndSelfTo<GreetingsScreenManager>().AsSingle();
			Container.BindInterfacesTo<GreetingsStartManager>().AsSingle();
			Container.BindInterfacesTo<GreetingQuitManager>().AsSingle();
			Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
			Container.Bind<RandomiserManager>().FromNewComponentOnNewGameObject().WithGameObjectName("GreetingsRandomiserManager").AsSingle().NonLazy();
			Container.Bind<FloorTextFloatingScreenController>().FromNewComponentAsViewController().AsSingle();
			Container.BindInterfacesAndSelfTo<RandomVideoFloatingScreenController>().FromNewComponentAsViewController().AsSingle();

			Container.Bind<GreetingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
			Container.Bind<NoVideosFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
			Container.Bind<NoVideosViewController>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<GreetingsSettingsViewController>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<ScreenControlsViewController>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<VideoSelectionViewController>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<YesNoModalViewController>().AsSingle();
		}
	}
}