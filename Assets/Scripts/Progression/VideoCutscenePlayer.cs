using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Progression
{
    /// <summary>
    /// Компонент для проигрывания финального видео.
    /// </summary>
    public class VideoCutscenePlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private UIDocument overlayDocument;
        [SerializeField] private AudioListener mainAudioListener;

        private VisualElement videoContainer;
        private VisualElement messageContainer;
        private Label messageLabel;
        private bool isPlaying = false;

        private void Awake()
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
                videoPlayer.targetCamera = Camera.main;
            }

            InitializeUI();
        }

        private void InitializeUI()
        {
            if (overlayDocument == null)
            {
                // Создать UI программно
                var uiGameObject = new GameObject("CutsceneOverlay");
                overlayDocument = uiGameObject.AddComponent<UIDocument>();
                
                var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.targetDisplay = 0;
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                overlayDocument.panelSettings = panelSettings;
            }

            var root = overlayDocument.rootVisualElement;
            
            // Контейнер для сообщения
            messageContainer = new VisualElement();
            messageContainer.name = "message-container";
            messageContainer.style.position = Position.Absolute;
            messageContainer.style.width = Length.Percent(100);
            messageContainer.style.height = Length.Percent(100);
            messageContainer.style.alignItems = Align.Center;
            messageContainer.style.justifyContent = Justify.Center;
            messageContainer.style.backgroundColor = new Color(0, 0, 0, 1);
            messageContainer.style.display = DisplayStyle.None;

            messageLabel = new Label();
            messageLabel.style.fontSize = 48;
            messageLabel.style.color = Color.white;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageContainer.Add(messageLabel);

            root.Add(messageContainer);
        }

        public void Play(VideoClip clip, string endMessage, float endMessageDuration, string nextSceneName, float delayBeforePlay = 0f)
        {
            if (isPlaying) return;

            StartCoroutine(PlayRoutine(clip, endMessage, endMessageDuration, nextSceneName, delayBeforePlay));
        }

        private IEnumerator PlayRoutine(VideoClip clip, string endMessage, float endMessageDuration, string nextSceneName, float delayBeforePlay)
        {
            isPlaying = true;

            // Заблокировать управление игроком
            var playerMovement = FindFirstObjectByType<PlayerMovements>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            // Задержка перед видео
            if (delayBeforePlay > 0f)
            {
                yield return new WaitForSeconds(delayBeforePlay);
            }

            // Настроить и запустить видео
            if (clip != null)
            {
                videoPlayer.clip = clip;
                videoPlayer.Play();

                // Ждать окончания видео
                while (videoPlayer.isPlaying)
                {
                    yield return null;
                }
            }

            // Показать сообщение
            if (!string.IsNullOrEmpty(endMessage) && messageContainer != null)
            {
                messageLabel.text = endMessage;
                messageContainer.style.display = DisplayStyle.Flex;
                yield return new WaitForSeconds(endMessageDuration);
            }

            // Загрузить следующую сцену
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }

            isPlaying = false;
        }
    }
}
