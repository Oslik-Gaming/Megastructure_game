using System.Collections;
using UnityEngine;

public class PlayAudioInterval : MonoBehaviour
{
    public AudioSource audioSource;
    public float interval = 5f; // Интервал в секундах между проигрываниями

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Запускаем корутину
        StartCoroutine(PlaySoundRoutine());
    }

    private IEnumerator PlaySoundRoutine()
    {
        while (true)
        {
            audioSource.Play();
            
            // Ждем указанное количество секунд перед следующим запуском
            yield return new WaitForSeconds(audioSource.clip.length + interval);
        }
    }
}
