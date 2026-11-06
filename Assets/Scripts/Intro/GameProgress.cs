using UnityEngine;

/// <summary>
/// GameProgress — прогресс прохождения игры. Хранится в PlayerPrefs,
/// переживает перезапуск игры.
///
/// Пока содержит только флаг просмотра вступительной катсцены,
/// по мере развития проекта сюда добавляются новые ключи (пройденные точки и т.д.).
/// </summary>
public static class GameProgress
{
    private const string KeyIntroSeen = "Progress_IntroSeen";
    private const string KeyCheckpoint = "Progress_CheckpointId";

    /// <summary>ID последнего активированного чекпоинта. -1 = чекпоинта нет (старт игры).</summary>
    public static int SavedCheckpointId => PlayerPrefs.GetInt(KeyCheckpoint, -1);

    /// <summary>Флаг запроса "Новая игра". Живёт только внутри запуска приложения:
    /// ставится в главном меню перед загрузкой сцены, съедается катсценой в SampleScene.</summary>
    public static bool NewGameRequested { get; private set; }

    /// <summary>Просмотрена ли вступительная катсцена.</summary>
    public static bool HasSeenIntro => PlayerPrefs.GetInt(KeyIntroSeen, 0) == 1;

    public static void MarkIntroSeen()
    {
        PlayerPrefs.SetInt(KeyIntroSeen, 1);
        PlayerPrefs.Save();
    }

    /// <summary>Вызывается из главного меню при нажатии "Новая игра":
    /// интро проиграется даже если оно уже было просмотрено.</summary>
    public static void RequestNewGame()
    {
        NewGameRequested = true;
    }

    /// <summary>Проверить и снять запрос новой игры. true — была нажата "Новая игра".</summary>
    public static bool ConsumeNewGameRequest()
    {
        bool requested = NewGameRequested;
        NewGameRequested = false;
        return requested;
    }

    /// <summary>Сохранить последний активированный чекпоинт (переживает перезапуск игры).</summary>
    public static void SaveCheckpoint(int checkpointId)
    {
        PlayerPrefs.SetInt(KeyCheckpoint, checkpointId);
        PlayerPrefs.Save();
    }

    /// <summary>Сбросить сохранённый чекпоинт (например, при "Новой игре").</summary>
    public static void ClearCheckpoint()
    {
        PlayerPrefs.DeleteKey(KeyCheckpoint);
        PlayerPrefs.Save();
    }

    /// <summary>Сбросить состояние мусора (вызывается при "Новой игре").</summary>
    public static void ResetTrash()
    {
        TrashPersistence.ResetAllTrash();
    }

    /// <summary>Полный сброс прогресса (пригодится для отладки).</summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KeyIntroSeen);
        PlayerPrefs.DeleteKey(KeyCheckpoint);
        TrashPersistence.ResetAllTrash();
        PlayerPrefs.Save();
    }
}
