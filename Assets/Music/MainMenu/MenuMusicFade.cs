using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicFade : MonoBehaviour
{
    private AudioSource audioSource;
    
    // Время в секундах, за которое музыка плавно включится
    [SerializeField] private float fadeDuration = 3f; 
    
    // Целевая громкость музыки
    [SerializeField] private float maxVolume = 1f; 

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Устанавливаем громкость в 0 перед стартом
        audioSource.volume = 0f; 
        
        // Запускаем плавное увеличение громкости
        StartCoroutine(FadeInMusic());
    }

    private IEnumerator FadeInMusic()
    {
        float currentTime = 0;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            // Плавно интерполируем громкость от 0 до максимальной
            audioSource.volume = Mathf.Lerp(0f, maxVolume, currentTime / fadeDuration);
            yield return null;
        }

        // Гарантируем точное значение в конце
        audioSource.volume = maxVolume; 
    }
}
