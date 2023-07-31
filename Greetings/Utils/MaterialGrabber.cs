using System.Linq;
using UnityEngine;

namespace Greetings.Utils
{
	internal sealed class MaterialGrabber
	{
		private Material? _noGlowRoundEdge;
		public Material NoGlowRoundEdge => _noGlowRoundEdge ??= Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UINoGlowRoundEdge");
	}
}