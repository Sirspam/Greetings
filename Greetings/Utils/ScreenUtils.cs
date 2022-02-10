using System.IO;
using System.Reflection;
using Greetings.Configuration;
using IPA.Utilities;
using SiraUtil.Extras;
using SiraUtil.Logging;
using Tweening;
using UnityEngine;
using UnityEngine.Video;
using Random = System.Random;

namespace Greetings.Utils
{
    internal class ScreenUtils
    {
        private readonly SiraLog _siraLog;
        private readonly PluginConfig _pluginConfig;
        private readonly SongPreviewPlayer _songPreviewPlayer;
        private readonly TimeTweeningManager _uwuTweenyManager;

        
        public VideoPlayer? VideoPlayer;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private readonly string _greetingsPath = Path.Combine(UnityGame.UserDataPath, nameof(Greetings));
        private Vector3 _screenScale;
        private Shader? _screenShader;
        private GameObject? _greetingsScreen;

        public ScreenUtils(SiraLog siraLog, PluginConfig pluginConfig, SongPreviewPlayer songPreviewPlayer, TimeTweeningManager timeTweeningManager)
        {
            _siraLog = siraLog;
            _pluginConfig = pluginConfig;
            _songPreviewPlayer = songPreviewPlayer;
            _uwuTweenyManager = timeTweeningManager;
        }

        public void ShowScreen(bool doTransition = true, bool playOnComplete = true, bool randomVideo = false)
        {
            CreateScreen(randomVideo);
            
            // Originally this was an anonymous delegate
            // Then I had to make a reference to the delegate so I could unsub from it
            // Then Rider suggested I make it into a local function
            // Now I'm reconsidering my life choices
            async void ShowScreenDelegate(VideoPlayer source)
            {
                VideoPlayer.prepareCompleted -= ShowScreenDelegate;
                VideoPlayer!.StepForward();

                // Sometimes greetings tries to start before the menu music starts to play, so fade out won't work and the background music will come in anyways
                var count = 1;
                while (_songPreviewPlayer.activeAudioClip == null && count <= 3)
                {
                    await Utilities.AwaitSleep(250);
                    count ++;
                }
                
                // This obviously could be improved as the player's aspect won't exactly fit the video's
                // Sadly I am too stupid to figure out a nice way to achieve that
                if (VideoPlayer.width > VideoPlayer.height)
                    _screenScale = new Vector3(4f, 2.5f, 0f);
                else if (VideoPlayer.width < VideoPlayer.height)
                    _screenScale = new Vector3(2f, 3f, 0f);
                else
                    _screenScale = new Vector3(2.5f, 2.5f, 0f);

                if (!doTransition)
                {
                    _greetingsScreen!.transform.localScale = _screenScale;
                    if (playOnComplete)
                    {
                        PlayAndFadeOutAudio();
                    }
                    return;
                }

                var tween = new FloatTween(0f, _screenScale.y, val => _greetingsScreen!.transform.localScale = new Vector3(_screenScale.x, val, _screenScale.z), 0.3f, EaseType.OutExpo)
                {
                    onCompleted = delegate
                    {
                        if (playOnComplete)
                        {
                            PlayAndFadeOutAudio();
                        }
                    }
                };
                _uwuTweenyManager.AddTween(tween, _greetingsScreen!.gameObject);
            }
            
            VideoPlayer!.Prepare();
            VideoPlayer.prepareCompleted += ShowScreenDelegate;
        }

        public void HideScreen(bool doTransition = true)
        {
            if (_greetingsScreen == null || VideoPlayer == null)
            {
                return;
            }

            VideoPlayer.Pause();
            _songPreviewPlayer.CrossfadeToDefault();

            if (!doTransition || _greetingsScreen.transform.localScale == Vector3.zero)
            {
                VideoPlayer.Stop();
                _greetingsScreen.transform.localScale = Vector3.zero;
                _greetingsScreen.SetActive(false);
                return;
            }
            var tween = new FloatTween(_screenScale.y, 0f, val => _greetingsScreen.transform.localScale = new Vector3(_screenScale.x, val, _screenScale.z), 0.3f, EaseType.OutExpo)
            {
                onCompleted = delegate
                {
                    VideoPlayer.Stop();
                    _greetingsScreen.SetActive(false);
                }
            };
            _uwuTweenyManager.AddTween(tween, _greetingsScreen.gameObject);
        }

        public void CreateScreen(bool randomVideo = false)
        {
            if (_greetingsScreen == null)
            {
                _siraLog.Info("Creating GreetingScreen");
                _greetingsScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _greetingsScreen.gameObject.name = "GreetingsScreen";
                _greetingsScreen.transform.position = new Vector3(0, 1.5f, 6f);
                _greetingsScreen.transform.localScale = Vector3.zero;
                
                VideoPlayer = _greetingsScreen.gameObject.AddComponent<VideoPlayer>();
                VideoPlayer.playOnAwake = false;
                VideoPlayer.renderMode = VideoRenderMode.MaterialOverride;
                
                var screenRenderer = _greetingsScreen.GetComponent<Renderer>();
                screenRenderer.material = new Material(GetShader())
                {
                    color = Color.white
                };
                screenRenderer.material.SetTexture(MainTex, VideoPlayer.texture);
                VideoPlayer.targetMaterialProperty = "_MainTex";
                VideoPlayer.targetMaterialRenderer = screenRenderer;
            }
            else
            {
                _greetingsScreen.SetActive(true);
            }
            
            if (randomVideo)
            {
                var files = Directory.GetFiles(_greetingsPath);
                var rand = new Random();
                VideoPlayer!.url = files[rand.Next(files.Length)];
            }
            else
            {
                VideoPlayer!.url = Path.Combine(_greetingsPath, _pluginConfig.SelectedVideo);
            }
        }

        private void PlayAndFadeOutAudio()
        {
            if (_greetingsScreen == null || !_greetingsScreen.activeSelf || _greetingsScreen.transform.localScale.y == 0f)
            {
                ShowScreen();
            }
            
            _songPreviewPlayer.FadeOut(0.8f);
            VideoPlayer!.Play();
        }
        

        private Shader GetShader()
        {
            if (_screenShader != null)
            {
                return _screenShader;
            }

            var loadedAssetBundle = AssetBundle.LoadFromMemory(BeatSaberMarkupLanguage.Utilities.GetResource(Assembly.GetExecutingAssembly(), "Greetings.Resources.greetings.bundle"));
            _screenShader = loadedAssetBundle.LoadAsset<Shader>("ScreenShader");
            loadedAssetBundle.Unload(false);
            return _screenShader;
        }
    }
}