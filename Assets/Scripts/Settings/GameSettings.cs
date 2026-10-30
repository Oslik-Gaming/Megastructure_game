using UnityEngine;

/// <summary>
/// GameSettings — глобальные настройки игры: общая громкость и чувствительность мыши.
///
/// Статический класс: работает из любой сцены, ничего не нужно вешать на объекты.
/// Значения хранятся в PlayerPrefs — автоматически сохраняются на диск и остаются
/// после выхода из игры. Загружаются сами при старте приложения.
/// </summary>
public static class GameSettings
{
    // ================= ДИАПАЗОНЫ =================

    public const float VolumeMin = 0f;
    public const float VolumeMax = 100f;
    public const float VolumeDefault = 100f;

    // Чувствительность показывается игроку как 0..100
    public const float SensitivityMin = 0f;
    public const float SensitivityMax = 100f;
    public const float SensitivityDefault = 40f; // 40 → множитель 2.0 (старое значение PlayerHeadLook)

    // ================= ПЕРЕВОД ЗНАЧЕНИЙ =================

    // Слайдер 0..100 → множитель для PlayerHeadLook: 1 единица слайдера = 0.05 множителя
    // (100 → 5.0, 40 → 2.0). Минимум 5 по слайдеру, чтобы мышью нельзя было "выключить" обзор.
    private const float SensitivityUnitsToMultiplier = 0.05f;
    private const float SensitivityEffectiveMin = 5f;

    // ================= КЛЮЧИ PlayerPrefs =================

    private const string KeyVolume = "Settings_MasterVolume";
    private const string KeySensitivity = "Settings_MouseSensitivity";

    // ================= ТЕКУЩИЕ ЗНАЧЕНИЯ =================

    /// <summary>Общая громкость: 0..100.</summary>
    public static float MasterVolume { get; private set; } = VolumeDefault;

    /// <summary>Чувствительность мыши: 0..100 (как на слайдере).</summary>
    public static float MouseSensitivity { get; private set; } = SensitivityDefault;

    /// <summary>Множитель чувствительности для PlayerHeadLook (0.25 .. 5.0).</summary>
    public static float SensitivityMultiplier =>
        Mathf.Max(SensitivityEffectiveMin, MouseSensitivity) * SensitivityUnitsToMultiplier;

    // ================= ЗАГРУЗКА / СОХРАНЕНИЕ =================

    // Вызывается автоматически один раз при запуске игры, до загрузки первой сцены.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoLoad() => Load();

    /// <summary>Загрузить сохранённые значения и применить их.</summary>
    public static void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(KeyVolume, VolumeDefault);
        MouseSensitivity = PlayerPrefs.GetFloat(KeySensitivity, SensitivityDefault);
        ApplyVolume();
    }

    /// <summary>Установить общую громкость (0..100), сохранить и применить сразу.</summary>
    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp(value, VolumeMin, VolumeMax);
        PlayerPrefs.SetFloat(KeyVolume, MasterVolume);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    /// <summary>Установить чувствительность мыши (0..100) и сохранить.</summary>
    public static void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, SensitivityMin, SensitivityMax);
        PlayerPrefs.SetFloat(KeySensitivity, MouseSensitivity);
        PlayerPrefs.Save();
    }

    // Громкость применяется ко всему звуку игры сразу (AudioListener глобальный).
    private static void ApplyVolume()
    {
        AudioListener.volume = MasterVolume / 100f;
    }
}
