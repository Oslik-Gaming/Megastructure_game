using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Progression
{
    /// <summary>
    /// Компонент для проигрывания финального видео.
    ///
    /// Видео рендерится в RenderTexture и показывается через полноэкранный RawImage
    /// на Canvas (Screen Space Overlay). Это работает в ЛЮБОМ рендер-пайплайне,
    /// включая URP, в отличие от VideoRenderMode.CameraFarPlane (с URP не рисует).
    ///
    /// Плеер создаётся автоматически (PlayCutsceneAction), настраивать вручную ничего не нужно.
    /// </summary>
    public class VideoCutscenePlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("Отладка")]
        [SerializeField] private bool debugLog = true;

        private Canvas canvas;
        private RawImage videoImage;
        private Text messageText;
        private RenderTexture renderTexture;
        private AudioSource videoAudioSource;
        private bool isPlaying = false;

        /// <summary>Каким был timeScale до катсцены (чтобы вернуть ровно как было).</summary>
        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
            }

            // Ловим ошибки видеоплеера, чтобы они не тонули в консоли без контекста
            videoPlayer.errorReceived += (source, message) =>
                Debug.LogError($"[VideoCutscenePlayer] Ошибка видео: {message}", this);

            // Звук видео — через отдельный AudioSource в режиме APIOnly:
            // он один не подчиняется AudioListener.pause и будет играть,
            // когда все остальные звуки игры заглушены.
            videoPlayer.audioOutputMode = VideoAudioOutputMode.APIOnly;
            videoAudioSource = gameObject.AddComponent<AudioSource>();
            videoAudioSource.playOnAwake = false;
            videoAudioSource.loop = false;
            videoAudioSource.spatialBlend = 0f; // 2D, громкость не зависит от позиции
            videoAudioSource.ignoreListenerPause = true;

            BuildOverlay();
        }

        /// <summary>Полноэкранный Canvas: RawImage для видео + надпись в конце.</summary>
        private void BuildOverlay()
        {
            if (canvas != null) return;

            var canvasObject = new GameObject("CutsceneCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Поверх всего остального UI
            canvas.sortingOrder = 32767;

            // Чёрный фон (виден во время задержки и после видео под надписью)
            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            var background = backgroundObject.AddComponent<Image>();
            background.color = Color.black;
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            // Полноэкранная картинка для видео (выключена — включается только когда есть текстура,
            // иначе RawImage рисует дефолтную БЕЛУЮ заливку)
            var imageObject = new GameObject("VideoImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            videoImage = imageObject.AddComponent<RawImage>();
            videoImage.enabled = false;
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            // Надпись по центру (показывается после видео)
            var textObject = new GameObject("MessageText");
            textObject.transform.SetParent(canvasObject.transform, false);
            messageText = textObject.AddComponent<Text>();
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.fontSize = 48;
            messageText.color = Color.white;
            messageText.font = GetBuiltinFont();
            messageText.enabled = false;

            // Пока видео не играет — оверлей не мешает
            canvasObject.SetActive(false);
        }

        /// <summary>Встроенный шрифт для надписи (LegacyRuntime в новых Unity, Arial в старых).</summary>
        private static Font GetBuiltinFont()
        {
            Font font;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            return font;
        }

        public void Play(VideoClip clip, string endMessage, float endMessageDuration, string nextSceneName, float delayBeforePlay = 0f)
        {
            if (isPlaying) return;

            StartCoroutine(PlayRoutine(clip, endMessage, endMessageDuration, nextSceneName, delayBeforePlay));
        }

        private IEnumerator PlayRoutine(VideoClip clip, string endMessage, float endMessageDuration, string nextSceneName, float delayBeforePlay)
        {
            isPlaying = true;

            // Заморозить мир: физика, апдейты, рост паразита — всё стоит (Time.deltaTime = 0).
            // И заглушить все звуки игры, оставив только звук видео.
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            // Заблокировать управление игроком
            var playerMovement = FindFirstObjectByType<PlayerMovements>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            // Показать оверлей (чёрный фон на время задержки, чтобы игрок не бегал по сцене)
            canvas.gameObject.SetActive(true);

            // Задержка перед видео (реальное время — timeScale = 0, обычный WaitForSeconds бы завис)
            if (delayBeforePlay > 0f)
            {
                yield return new WaitForSecondsRealtime(delayBeforePlay);
            }

            if (clip != null)
            {
                // Рендерим в RenderTexture — работает и на Built-in, и на URP
                EnsureRenderTexture();
                videoPlayer.targetTexture = renderTexture;
                videoImage.texture = renderTexture;
                videoImage.enabled = true;
                messageText.enabled = false;

                videoPlayer.clip = clip;
                videoPlayer.SetTargetAudioSource(0, videoAudioSource);
                videoPlayer.Play();

                if (debugLog) Debug.Log($"[VideoCutscenePlayer] Играем видео '{clip.name}' " +
                                        $"({videoPlayer.length:0}s), звук и картинка через RenderTexture.", this);

                // Ждать окончания видео, раз в секунду — диагностика
                float waitLogTimer = 0f;
                long lastLoggedFrame = -1;
                while (videoPlayer.isPlaying)
                {
                    waitLogTimer += Time.unscaledDeltaTime;
                    if (debugLog && waitLogTimer >= 1f)
                    {
                        waitLogTimer = 0f;
                        Debug.Log($"[VideoCutscenePlayer] Видео идёт: кадр {videoPlayer.frame}, время {videoPlayer.time:0.0}/{videoPlayer.length:0.0}s.", this);
                        if (videoPlayer.frame == lastLoggedFrame)
                            Debug.LogWarning($"[VideoCutscenePlayer] Кадр не меняется ({videoPlayer.frame}) — видео зависло. Проверь кодек/формат клипа.", this);
                        lastLoggedFrame = videoPlayer.frame;
                    }
                    yield return null;
                }

                if (debugLog)
                    Debug.Log($"[VideoCutscenePlayer] Видео закончилось: кадр {videoPlayer.frame}, время {videoPlayer.time:0.0}s.", this);

                // Спрятать видео, оставить чёрный фон под надпись
                videoPlayer.targetTexture = null;
                videoImage.enabled = false;
                videoAudioSource.Stop();
            }
            else
            {
                Debug.LogWarning("[VideoCutscenePlayer] Видео не указано — пропускаю проигрывание.", this);
            }

            // Показать сообщение
            if (!string.IsNullOrEmpty(endMessage) && messageText != null)
            {
                messageText.text = endMessage;
                messageText.enabled = true;
                yield return new WaitForSecondsRealtime(endMessageDuration);
            }

            // Спрятать оверлей
            canvas.gameObject.SetActive(false);

            // Разморозить мир и вернуть звуки ДО загрузки сцены
            Unfreeze();

            // Загрузить следующую сцену
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                try
                {
                    if (debugLog) Debug.Log($"[VideoCutscenePlayer] Загружаю сцену '{nextSceneName}'.", this);
                    SceneManager.LoadScene(nextSceneName);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[VideoCutscenePlayer] Не удалось загрузить сцену '{nextSceneName}': {e.Message}\n" +
                                   "Скорее всего сцена не добавлена в Build Settings (File → Build Profiles → Scene List).", this);
                }
            }

            isPlaying = false;
        }

        /// <summary>Вернуть время и звуки как было до катсцены.</summary>
        private void Unfreeze()
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
            videoAudioSource.Stop();
        }

        /// <summary>Создать/обновить RenderTexture под текущее разрешение экрана.</summary>
        private void EnsureRenderTexture()
        {
            int width = Mathf.Max(2, Screen.width);
            int height = Mathf.Max(2, Screen.height);

            if (renderTexture != null && (renderTexture.width != width || renderTexture.height != height))
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                renderTexture.name = "CutsceneVideoRT";
            }
        }

        private void OnDestroy()
        {
            // Страховка: если плеер уничтожили посреди катсцены — не оставить игру замороженной навсегда
            Unfreeze();

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }
    }
}
