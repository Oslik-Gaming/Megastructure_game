using UnityEngine;
using Progression;

/// <summary>
/// Пример программной настройки системы прогрессии.
/// 
/// ВАЖНО: Этот класс показывает, как настроить шаги в инспекторе.
/// Из-за того, что IStepAction и IStepCondition - интерфейсы с приватными полями,
/// настройка должна производиться через Inspector в GameProgressionManager.
/// 
/// Используйте этот файл как справочник для настройки шагов вручную.
/// </summary>
public class ProgressionSetupExample : MonoBehaviour
{
    [Header("СПРАВОЧНАЯ ИНФОРМАЦИЯ")]
    [TextArea(20, 50)]
    public string setupInstructions = @"
НАСТРОЙКА СИСТЕМЫ ПРОГРЕССИИ

1. На GameObject с GameProgressionManager настроить массив Steps (Size = 10):

ШАГ 0: Подбор Выжигателя
  Step Id: step_pickup_heater
  Task Text: AI: Добро пожаловать. Возьми Выжигатель.
  Entry Actions:
    - ShowTaskAction (taskText = задание выше)
    - SpawnToolAction (toolType=Heater, spawnPoint, prefab)
  Conditions:
    - ToolPickedCondition (toolType=Heater)

ШАГ 1: Очистка локации 1
  Step Id: step_clean_location1
  Task Text: Очисти локацию 1
  Entry Actions:
    - ShowTaskAction
  Conditions:
    - LocationCleanedCondition (location = TrashLocation 1)

ШАГ 2: Подбор Деструктора
  Step Id: step_pickup_destructor
  Task Text: Возьми Деструктор
  Entry Actions:
    - ShowTaskAction
    - SpawnToolAction (toolType=Destructor)
  Conditions:
    - ToolPickedCondition (toolType=Destructor)

ШАГ 3: Разрушение камня
  Step Id: step_destroy_rock
  Task Text: Разрушь большой камень
  Entry Actions:
    - ShowTaskAction
  Conditions:
    - ObjectDestroyedCondition (target = BigRock)

ШАГ 4: Переход в локацию 2
  Step Id: step_enter_location2
  Task Text: Перейди в локацию 2
  Entry Actions:
    - ShowTaskAction
    - OpenDoorAction (door = Door2)
    - ShowDirectionArrowAction (target = Entrance2)
  Conditions:
    - ZoneEnteredCondition (triggerZone = TriggerZone2)

ШАГ 5: Очистка локации 2
  Step Id: step_clean_location2
  Task Text: Очисти локацию 2
  Entry Actions:
    - ShowTaskAction
    - SwitchLocationTrackingAction (newLocation = Location2)
    - HideDirectionArrowAction
  Conditions:
    - LocationCleanedCondition (location = Location2)

ШАГ 6: Подбор Насоса
  Step Id: step_pickup_sprayer
  Task Text: Возьми Насос
  Entry Actions:
    - ShowTaskAction
    - SpawnToolAction (toolType=Sprayer)
  Conditions:
    - ToolPickedCondition (toolType=Sprayer)

ШАГ 7: Переход в локацию 3
  Step Id: step_enter_location3
  Task Text: Перейди в локацию 3
  Entry Actions:
    - ShowTaskAction
    - OpenDoorAction (door = Door3)
    - ShowDirectionArrowAction (target = Entrance3)
  Conditions:
    - ZoneEnteredCondition (triggerZone = TriggerZone3)

ШАГ 8: Очистка локации 3
  Step Id: step_clean_location3
  Task Text: Очисти локацию 3
  Entry Actions:
    - ShowTaskAction
    - SwitchLocationTrackingAction (newLocation = Location3)
    - HideDirectionArrowAction
  Conditions:
    - LocationCleanedCondition (location = Location3)

ШАГ 9: Финальное видео
  Step Id: step_ending
  Task Text: Что-то не так... Беги
  Entry Actions:
    - ShowTaskAction
    - PlayCutsceneAction (video, endMessage, nextSceneName=MainGameMenu)
  Conditions: (пусто - финальный шаг)
";
}
