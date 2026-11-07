using System;
using UnityEngine;
using UnityEngine.Video;

namespace Progression.Actions
{
    [Serializable]
    public class PlayCutsceneAction : IStepAction
    {
        [SerializeField] private VideoClip video;
        [SerializeField] private string endMessage = "Спасибо за игру";
        [SerializeField] private float endMessageDuration = 3f;
        [SerializeField] private string nextSceneName = "MainGameMenu";
        [SerializeField] private float delayBeforePlay = 3f;

        public void Execute(GameProgressionManager context)
        {
            var cutscenePlayer = GameObject.FindFirstObjectByType<VideoCutscenePlayer>();
            
            if (cutscenePlayer == null)
            {
                // Создать плеер программно
                var playerObject = new GameObject("VideoCutscenePlayer");
                cutscenePlayer = playerObject.AddComponent<VideoCutscenePlayer>();
            }

            if (video != null)
            {
                cutscenePlayer.Play(video, endMessage, endMessageDuration, nextSceneName, delayBeforePlay);
            }
            else
            {
                Debug.LogWarning("[PlayCutsceneAction] Видео не указано.");
            }
        }

        public void Restore(GameProgressionManager context)
        {
            // Катсцена не восстанавливается при загрузке - она запускается только один раз
        }
    }
}
