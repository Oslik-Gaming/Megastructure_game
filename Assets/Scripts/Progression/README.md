# Система машины состояний игрового процесса

## Обзор

Система машины состояний управляет прогрессом игры через последовательность шагов. Каждый шаг имеет:
- **Условия завершения** - что должно произойти для завершения шага
- **Действия при входе** - что происходит при активации шага

## Созданные компоненты

### Основные классы (Assets/Scripts/Progression/)
- `GameProgressionManager.cs` - главный менеджер прогрессии
- `ProgressionStep.cs` - класс шага с условиями и действиями
- `IStepCondition.cs` - интерфейс условий
- `IStepAction.cs` - интерфейс действий
- `ToolType.cs` - enum типов инструментов
- `ToolInventory.cs` - хранит разблокированные инструменты
- `PickupableTool.cs` - инструмент для подбора в мире
- `DirectionArrow.cs` - стрелка направления в HUD
- `Door.cs` - открываемая дверь
- `VideoCutscenePlayer.cs` - плеер финального видео
- `ProgressionTriggerZone.cs` - триггер-зона для входа в локации

### Условия (Assets/Scripts/Progression/Conditions/)
- `ToolPickedCondition` - инструмент подобран
- `LocationCleanedCondition` - локация очищена
- `ObjectDestroyedCondition` - объект разрушен
- `ZoneEnteredCondition` - игрок вошёл в зону

### Действия (Assets/Scripts/Progression/Actions/)
- `ShowTaskAction` - показать текст задания
- `SpawnToolAction` - заспавнить инструмент для подбора
- `SwitchLocationTrackingAction` - переключить отслеживаемую локацию
- `ShowDirectionArrowAction` - показать стрелку направления
- `HideDirectionArrowAction` - скрыть стрелку
- `OpenDoorAction` - открыть дверь
- `PlayCutsceneAction` - запустить видео
- `LoadSceneAction` - загрузить сцену

## Настройка в сцене GameStateMachineTest

### 1. Создать основные объекты

#### GameProgressionManager
1. Создать пустой GameObject в сцене: `GameObject → Create Empty`
2. Назвать: `GameProgressionManager`
3. Добавить компонент: `GameProgressionManager`

#### ToolInventory на игроке
1. Найти объект игрока (с компонентом `PlayerMovements`)
2. Добавить компонент: `ToolInventory`

#### DirectionArrow
1. Найти объект с компонентом `GameHud`
2. Добавить компонент: `DirectionArrow`
3. В инспекторе указать:
   - `Hud Document` - ссылка на UIDocument из GameHud
   - `Player Camera` - Main Camera

### 2. Создать точки спавна инструментов

Для каждого инструмента создать пустой GameObject:
- `ToolSpawnPoint_Heater` (поместить в локацию 1)
- `ToolSpawnPoint_Destructor` (поместить в локацию 2)
- `ToolSpawnPoint_Sprayer` (поместить в локацию 3)

### 3. Создать префабы инструментов для подбора

Для каждого инструмента:
1. Создать GameObject с визуальной моделью инструмента
2. Добавить компонент `PickupableTool`
3. Настроить:
   - `Tool Type` - тип инструмента
   - `Visual Model` - ссылка на модель
   - `Pickup Anim Duration` - длительность анимации подбора
4. Сохранить как префаб в `Assets/Prefabs/Tools/`

### 4. Создать триггер-зоны входа в локации

Для локаций 2 и 3:
1. Создать GameObject
2. Добавить `Box Collider`
3. Настроить:
   - `Is Trigger` = true
   - Размер и позиция на входе в локацию
4. Добавить компонент `ProgressionTriggerZone`
5. Убедиться, что игрок имеет тег `Player`

### 5. Создать двери

Для локаций 2 и 3:
1. Создать GameObject для двери
2. Добавить компонент `Door`
3. Настроить:
   - `Door Model` - Transform двери (что будет двигаться)
   - `Open Offset` - вектор сдвига при открытии (например, `(0, 3, 0)`)
   - `Open Duration` - длительность анимации

### 6. Настроить шаги прогрессии

В компоненте `GameProgressionManager` настроить 10 шагов:

#### Шаг 0: Подбор Выжигателя
- **Step Id**: `step_pickup_heater`
- **Task Text**: `AI: Добро пожаловать. Возьми Выжигатель.`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `AI: Добро пожаловать. Возьми Выжигатель.`
  - `SpawnToolAction` → Tool Type = `Heater`, Spawn Point = `ToolSpawnPoint_Heater`, Pickup Prefab = префаб
- **Conditions**:
  - `ToolPickedCondition` → Tool Type = `Heater`

#### Шаг 1: Очистка локации 1
- **Step Id**: `step_clean_location1`
- **Task Text**: `Очисти локацию 1`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Очисти локацию 1`
- **Conditions**:
  - `LocationCleanedCondition` → Location = `TrashLocation` локации 1

#### Шаг 2: Подбор Деструктора
- **Step Id**: `step_pickup_destructor`
- **Task Text**: `Возьми Деструктор`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Возьми Деструктор`
  - `SpawnToolAction` → Tool Type = `Destructor`, Spawn Point = `ToolSpawnPoint_Destructor`, Pickup Prefab = префаб
- **Conditions**:
  - `ToolPickedCondition` → Tool Type = `Destructor`

#### Шаг 3: Разрушение камня
- **Step Id**: `step_destroy_rock`
- **Task Text**: `Разрушь большой камень`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Разрушь большой камень`
- **Conditions**:
  - `ObjectDestroyedCondition` → Target = конкретный `BigRock`

#### Шаг 4: Переход в локацию 2
- **Step Id**: `step_enter_location2`
- **Task Text**: `Перейди в локацию 2`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Перейди в локацию 2`
  - `OpenDoorAction` → Door = дверь локации 2
  - `ShowDirectionArrowAction` → Target = вход в локацию 2
- **Conditions**:
  - `ZoneEnteredCondition` → Trigger Zone = коллайдер входа в локацию 2

#### Шаг 5: Очистка локации 2
- **Step Id**: `step_clean_location2`
- **Task Text**: `Очисти локацию 2`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Очисти локацию 2`
  - `SwitchLocationTrackingAction` → New Location = `TrashLocation` локации 2
  - `HideDirectionArrowAction`
- **Conditions**:
  - `LocationCleanedCondition` → Location = `TrashLocation` локации 2

#### Шаг 6: Подбор Насоса
- **Step Id**: `step_pickup_sprayer`
- **Task Text**: `Возьми Насос`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Возьми Насос`
  - `SpawnToolAction` → Tool Type = `Sprayer`, Spawn Point = `ToolSpawnPoint_Sprayer`, Pickup Prefab = префаб
- **Conditions**:
  - `ToolPickedCondition` → Tool Type = `Sprayer`

#### Шаг 7: Переход в локацию 3
- **Step Id**: `step_enter_location3`
- **Task Text**: `Перейди в локацию 3`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Перейди в локацию 3`
  - `OpenDoorAction` → Door = дверь локации 3
  - `ShowDirectionArrowAction` → Target = вход в локацию 3
- **Conditions**:
  - `ZoneEnteredCondition` → Trigger Zone = коллайдер входа в локацию 3

#### Шаг 8: Очистка локации 3
- **Step Id**: `step_clean_location3`
- **Task Text**: `Очисти локацию 3`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Очисти локацию 3`
  - `SwitchLocationTrackingAction` → New Location = `TrashLocation` локации 3
  - `HideDirectionArrowAction`
- **Conditions**:
  - `LocationCleanedCondition` → Location = `TrashLocation` локации 3

#### Шаг 9: Финальное видео
- **Step Id**: `step_ending`
- **Task Text**: `Что-то не так... Беги`
- **Entry Actions**:
  - `ShowTaskAction` → Task Text = `Что-то не так... Беги`
  - `PlayCutsceneAction`:
    - Video = (импортировать видео позже)
    - End Message = `Спасибо за игру в бетатест`
    - End Message Duration = `3`
    - Next Scene Name = `MainGameMenu`
    - Delay Before Play = `3`
- **Conditions**: (оставить пустым - финальный шаг)

### 7. Обновить ToolController

В объекте ToolAnchor игрока компонент `ToolController` нужно настроить:
- Убедиться, что в массиве `tools` есть все 4 инструмента
- Массив `toolTypes` должен соответствовать порядку:
  1. GravityGun
  2. Heater
  3. Destructor
  4. Sprayer

## Важные замечания

### Сохранение состояния
- Прогресс сохраняется автоматически при переходе между шагами
- При выходе в меню и возврате состояние восстанавливается через `Restore()`
- "Новая игра" сбрасывает все сохранённые данные

### Идемпотентность
Все действия должны корректно работать при многократном вызове `Restore()`:
- `SpawnToolAction` не спавнит инструмент, если он уже подобран
- `OpenDoorAction` открывает дверь только если она закрыта
- `ShowTaskAction` просто устанавливает текст (безопасно)

### Тег Player
Убедитесь, что объект игрока имеет тег `Player` - это критично для работы `ProgressionTriggerZone`.

### Начальные инструменты
Согласно плану, GravityGun доступен с самого начала. В `ToolInventory.Awake()` можно добавить:
```csharp
UnlockTool(ToolType.GravityGun); // Гравитационная пушка доступна с начала
```

## Тестирование

1. **Запустить сцену** - все инструменты должны быть скрыты кроме GravityGun
2. **Подобрать Heater** - инструмент появится в слоте 2 (клавиша 2)
3. **Очистить локацию 1** - шаг автоматически завершится
4. **Умереть и воскреснуть** - прогресс должен сохраниться
5. **Выйти в меню и "Продолжить"** - прогресс восстановится
6. **"Новая игра"** - все сбросится, начнётся с шага 0

## Отладка

Для отладки можно:
- Проверить текущий шаг: `GameProgress.LoadProgressionStep()`
- Вручную установить шаг: `GameProgress.SaveProgressionStep(N)`
- Сбросить всё: `GameProgress.ResetProgress()`
