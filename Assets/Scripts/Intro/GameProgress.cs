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
    private const string KeyProgressionStep = "Progress_StepIndex";
    private const string KeyProgressGeneration = "Progress_Generation";
    private const string KeyPlayerSave = "Progress_PlayerSave";
    private const string KeyPlayerScene = "Progress_PlayerScene";
    private const string KeyPlayerPosition = "Progress_PlayerPosition";
    private const string KeyPlayerRotation = "Progress_PlayerRotation";
    private const string KeyPlayerHeadRotation = "Progress_PlayerHeadRotation";
    private const string KeyGameCompleted = "Progress_GameCompleted";

    public static int ProgressGeneration => PlayerPrefs.GetInt(KeyProgressGeneration, 0);

    private static string GenerationKey(string key) =>
        $"{key}_Gen{ProgressGeneration}";

    public static bool HasSavedPlayerTransform =>
        PlayerPrefs.GetInt(GenerationKey(KeyPlayerSave), 0) == 1;

    public static string SavedPlayerScene =>
        PlayerPrefs.GetString(GenerationKey(KeyPlayerScene), string.Empty);

    public static bool IsGameCompleted =>
        PlayerPrefs.GetInt(GenerationKey(KeyGameCompleted), 0) == 1;

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

    /// <summary>Starts a new save generation without deleting old PlayerPrefs keys.</summary>
    public static void StartNewGame()
    {
        PlayerPrefs.SetInt(KeyProgressGeneration, ProgressGeneration + 1);
        PlayerPrefs.Save();
    }

    /// <summary>Сохраняет положение игрока для текущего поколения игры.</summary>
    public static void SavePlayerTransform(
        string sceneName,
        Vector3 position,
        Quaternion rotation,
        Quaternion headLocalRotation)
    {
        PlayerPrefs.SetInt(GenerationKey(KeyPlayerSave), 1);
        PlayerPrefs.SetString(GenerationKey(KeyPlayerScene), sceneName);

        SetVector3(KeyPlayerPosition, position);
        SetQuaternion(KeyPlayerRotation, rotation);
        SetQuaternion(KeyPlayerHeadRotation, headLocalRotation);
        PlayerPrefs.Save();
    }

    /// <summary>Удаляет сохранённое положение игрока текущего поколения.</summary>
    public static void ClearPlayerSave()
    {
        PlayerPrefs.DeleteKey(GenerationKey(KeyPlayerSave));
        PlayerPrefs.DeleteKey(GenerationKey(KeyPlayerScene));

        DeleteVector3(KeyPlayerPosition);
        DeleteQuaternion(KeyPlayerRotation);
        DeleteQuaternion(KeyPlayerHeadRotation);
        PlayerPrefs.Save();
    }

    /// <summary>Помечает текущее прохождение завершённым.</summary>
    public static void MarkGameCompleted()
    {
        PlayerPrefs.SetInt(GenerationKey(KeyGameCompleted), 1);
        ClearPlayerSave();
        PlayerPrefs.Save();
    }

    /// <summary>Загружает сохранённое положение игрока текущего поколения.</summary>
    public static bool TryLoadPlayerTransform(
        out Vector3 position,
        out Quaternion rotation,
        out Quaternion headLocalRotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        headLocalRotation = Quaternion.identity;

        if (!HasSavedPlayerTransform) return false;

        position = new Vector3(
            PlayerPrefs.GetFloat(GenerationKey(KeyPlayerPosition + "X")),
            PlayerPrefs.GetFloat(GenerationKey(KeyPlayerPosition + "Y")),
            PlayerPrefs.GetFloat(GenerationKey(KeyPlayerPosition + "Z")));
        rotation = ReadQuaternion(KeyPlayerRotation);
        headLocalRotation = ReadQuaternion(KeyPlayerHeadRotation);
        return true;
    }

    private static void SetVector3(string key, Vector3 value)
    {
        PlayerPrefs.SetFloat(GenerationKey(key + "X"), value.x);
        PlayerPrefs.SetFloat(GenerationKey(key + "Y"), value.y);
        PlayerPrefs.SetFloat(GenerationKey(key + "Z"), value.z);
    }

    private static void SetQuaternion(string key, Quaternion value)
    {
        PlayerPrefs.SetFloat(GenerationKey(key + "X"), value.x);
        PlayerPrefs.SetFloat(GenerationKey(key + "Y"), value.y);
        PlayerPrefs.SetFloat(GenerationKey(key + "Z"), value.z);
        PlayerPrefs.SetFloat(GenerationKey(key + "W"), value.w);
    }

    private static Quaternion ReadQuaternion(string key)
    {
        return new Quaternion(
            PlayerPrefs.GetFloat(GenerationKey(key + "X")),
            PlayerPrefs.GetFloat(GenerationKey(key + "Y")),
            PlayerPrefs.GetFloat(GenerationKey(key + "Z")),
            PlayerPrefs.GetFloat(GenerationKey(key + "W")));
    }

    private static void DeleteVector3(string key)
    {
        PlayerPrefs.DeleteKey(GenerationKey(key + "X"));
        PlayerPrefs.DeleteKey(GenerationKey(key + "Y"));
        PlayerPrefs.DeleteKey(GenerationKey(key + "Z"));
    }

    private static void DeleteQuaternion(string key)
    {
        DeleteVector3(key);
        PlayerPrefs.DeleteKey(GenerationKey(key + "W"));
    }

    /// <summary>Полный сброс прогресса (пригодится для отладки).</summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KeyIntroSeen);
        PlayerPrefs.DeleteKey(KeyCheckpoint);
        PlayerPrefs.DeleteKey(KeyProgressionStep);
        PlayerPrefs.DeleteKey(KeyProgressionChain);
        PlayerPrefs.DeleteKey(KeyProgressGeneration);
        PlayerPrefs.DeleteKey("Progress_UnlockedTools");
        
        // Сбросить подбор инструментов
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.DeleteKey($"Progress_Tool_{i}_Picked");
        }
        
        // Сбросить двери (по общему префиксу удалить не можем, но при новой игре они пересоздадутся)
        
        TrashPersistence.ResetAllTrash();
        PlayerPrefs.Save();
    }

    /// <summary>Отпечаток цепочки шагов прогрессии (чтобы сейв старого списка не подменял шаги нового).</summary>
    private const string KeyProgressionChain = "Progress_StepsHash";

    /// <summary>Сохранить отпечаток цепочки шагов (вызывается вместе с шагом).</summary>
    public static void SaveProgressionChain(string chainHash)
    {
        PlayerPrefs.SetString(KeyProgressionChain, chainHash ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>Загрузить отпечаток цепочки шагов. Пустая строка — не сохранялся.</summary>
    public static string LoadProgressionChain()
    {
        return PlayerPrefs.GetString(KeyProgressionChain, "");
    }

    /// <summary>Сохранить текущий шаг прогрессии.</summary>
    public static void SaveProgressionStep(int stepIndex)
    {
        PlayerPrefs.SetInt(KeyProgressionStep, stepIndex);
        PlayerPrefs.Save();
    }

    /// <summary>Загрузить сохранённый шаг прогрессии. Возвращает 0, если шаг не сохранён.</summary>
    public static int LoadProgressionStep()
    {
        return PlayerPrefs.GetInt(KeyProgressionStep, 0);
    }

    /// <summary>Сбросить шаг прогрессии (вызывается при "Новой игре").</summary>
    public static void ResetProgressionStep()
    {
        PlayerPrefs.DeleteKey(KeyProgressionStep);
        PlayerPrefs.DeleteKey(KeyProgressionChain);
        PlayerPrefs.Save();
    }
}
