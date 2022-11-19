using HMUI;
using IPA.Utilities;

namespace Greetings.Utils
{
	internal sealed class Accessors
	{
		public static readonly FieldAccessor<ModalView, bool>.Accessor ViewValidAccessor =
			FieldAccessor<ModalView, bool>.GetAccessor("_viewIsValid");
		
		public static readonly FieldAccessor<GameplaySetupViewController, ColorsOverrideSettingsPanelController>.Accessor ColorsPanelAccessor =
			FieldAccessor<GameplaySetupViewController, ColorsOverrideSettingsPanelController>.GetAccessor("_colorsOverrideSettingsPanelController");
		
		public static readonly FieldAccessor<ColorsOverrideSettingsPanelController, PanelAnimationSO>.Accessor PresentAnimationAccessor =
			FieldAccessor<ColorsOverrideSettingsPanelController, PanelAnimationSO>.GetAccessor("_presentPanelAnimation");

		public static readonly FieldAccessor<ColorsOverrideSettingsPanelController, PanelAnimationSO>.Accessor DismissAnimationAccessor =
			FieldAccessor<ColorsOverrideSettingsPanelController, PanelAnimationSO>.GetAccessor("_dismissPanelAnimation");
	}
}