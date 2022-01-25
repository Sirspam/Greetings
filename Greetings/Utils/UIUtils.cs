using System.Threading.Tasks;
using HMUI;
using Tweening;
using UnityEngine;

namespace Greetings.Utils
{
    internal class UIUtils
    {
        private readonly TimeTweeningManager _uwuTweenyManager; // Thanks once again, PixelBoom

        public UIUtils(TimeTweeningManager timeTweeningManager)
        {
            _uwuTweenyManager = timeTweeningManager;
        }

        public async void ButtonUnderlineClick(GameObject gameObject)
        {
            var underline = await Task.Run(() => gameObject.transform.Find("Underline").gameObject.GetComponent<ImageView>());

            _uwuTweenyManager.KillAllTweens(underline);

            var tween = new FloatTween(0f, 1f, val => underline.color = Color.Lerp(new Color(0f, 0.7f, 1f), new Color(1f, 1f, 1f, 0.502f), val), 1f, EaseType.InSine);
            _uwuTweenyManager.AddTween(tween, underline);
        }
    }
}