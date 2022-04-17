using HMUI;
using IPA.Utilities;

namespace Greetings.Utils
{
	internal class Accessors
	{
		public static readonly FieldAccessor<ModalView, bool>.Accessor ViewValidAccessor = FieldAccessor<ModalView, bool>.GetAccessor("_viewIsValid");
	}
}