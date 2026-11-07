# Решение проблемы отображения в Inspector

## Проблема
В Unity Inspector списки `Entry Actions` и `Conditions` отображались как нередактируемые плашки "Element 0", "Element 1" и т.д., потому что Unity не может напрямую сериализовать интерфейсы.

## Решение
Система использует два подхода:

### 1. Атрибут [SerializeReference] (Unity 2019.3+)
В `ProgressionStep.cs`:
```csharp
[SerializeReference] public List<IStepAction> entryActions = new List<IStepAction>();
[SerializeReference] public List<IStepCondition> conditions = new List<IStepCondition>();
```

Этот атрибут позволяет Unity сериализовать полиморфные типы (классы, реализующие интерфейсы).

### 2. Кастомный PropertyDrawer
Файл `Assets/Scripts/Progression/Editor/ProgressionStepDrawer.cs` предоставляет удобный интерфейс для редактирования шагов прогрессии в Inspector:

- **Развёртываемые секции** для Entry Actions и Conditions
- **Кнопка "Add"** с выпадающим меню для выбора типа действия/условия
- **Кнопка "Delete"** для удаления элементов
- **Автоматическое отображение** всех полей каждого действия/условия

## Использование в Inspector

### Добавление шага
1. Выберите GameObject с компонентом `GameProgressionManager`
2. Увеличьте размер списка `Steps`
3. Раскройте новый элемент
4. Заполните `Step Id` и `Task Text`

### Добавление действия (Entry Actions)
1. Раскройте секцию `Entry Actions`
2. Нажмите кнопку `Add Entry Actions`
3. Выберите нужное действие из списка:
   - ShowTaskAction
   - SpawnToolAction
   - SwitchLocationTrackingAction
   - ShowDirectionArrowAction
   - HideDirectionArrowAction
   - OpenDoorAction
   - PlayCutsceneAction
   - LoadSceneAction

### Добавление условия (Conditions)
1. Раскройте секцию `Conditions`
2. Нажмите кнопку `Add Conditions`
3. Выберите нужное условие из списка:
   - ToolPickedCondition
   - LocationCleanedCondition
   - ObjectDestroyedCondition
   - ZoneEnteredCondition

## Доступные типы действий

| Действие | Описание | Параметры |
|----------|----------|-----------|
| ShowTaskAction | Отобразить задание в HUD | gameHud |
| SpawnToolAction | Заспавнить инструмент | toolType, spawnPoint, pickupPrefab |
| SwitchLocationTrackingAction | Переключить отслеживаемую локацию | newLocation |
| ShowDirectionArrowAction | Показать стрелку направления | target |
| HideDirectionArrowAction | Скрыть стрелку направления | - |
| OpenDoorAction | Открыть дверь | door |
| PlayCutsceneAction | Запустить видео | video, endMessage, endMessageDuration, nextSceneName, delayBeforePlay |
| LoadSceneAction | Загрузить сцену | sceneName |

## Доступные типы условий

| Условие | Описание | Параметры |
|---------|----------|-----------|
| ToolPickedCondition | Инструмент подобран | toolType |
| LocationCleanedCondition | Локация очищена | location |
| ObjectDestroyedCondition | Объект разрушен | target |
| ZoneEnteredCondition | Вход в зону | triggerZone |

## Пример настройки

### Шаг 0: Подобрать первый мусор
```
Step Id: step_0_first_trash
Task Text: Подобрать первый мусор

Entry Actions:
  - ShowTaskAction
    - gameHud: <ссылка на GameHud>

Conditions:
  - ObjectDestroyedCondition
    - target: <ссылка на DamageableTrashItem>
```

### Шаг 1: Подобрать гравитационную пушку
```
Step Id: step_1_gravity_gun
Task Text: Подобрать гравитационную пушку

Entry Actions:
  - SpawnToolAction
    - toolType: GravityGun
    - spawnPoint: <Transform точки спавна>
    - pickupPrefab: <Prefab PickupableTool>
  - ShowDirectionArrowAction
    - target: <Transform заспавненного инструмента>

Conditions:
  - ToolPickedCondition
    - toolType: GravityGun
```

## Устранение неполадок

### Действия/условия не отображаются в Inspector
- Убедитесь, что папка `Assets/Scripts/Progression/Editor` существует
- Проверьте, что файл `ProgressionStepDrawer.cs` находится именно в папке `Editor`
- Перезапустите Unity Editor

### Кнопка "Add" не работает
- Убедитесь, что все классы действий/условий имеют публичный конструктор без параметров
- Проверьте, что типы добавлены в методы `GetActionTypes()` и `GetConditionTypes()` в `ProgressionStepDrawer.cs`

### Поля действий/условий не сериализуются
- Убедитесь, что все поля помечены атрибутом `[SerializeField]`
- Используйте только сериализуемые типы Unity (примитивы, UnityEngine.Object, другие [Serializable] классы)
