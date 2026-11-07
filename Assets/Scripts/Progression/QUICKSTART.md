# Быстрый старт - тестирование системы прогрессии

## Шаг 1: Открыть тестовую сцену
Открыть `Assets/Scenes/GameStateMachineTest.unity`

## Шаг 2: Добавить необходимые компоненты

### GameProgressionManager
1. Создать пустой GameObject: `Create Empty`
2. Назвать: `GameProgressionManager`
3. Добавить компонент: `Progression.GameProgressionManager`

### ToolInventory на игроке
1. Найти объект игрока (с `PlayerMovements`)
2. Добавить компонент: `Progression.ToolInventory`

### DirectionArrow
1. Найти объект с `GameHud`
2. Добавить компонент: `Progression.DirectionArrow`
3. Настроить:
   - Hud Document: UIDocument из GameHud
   - Player Camera: Main Camera

## Шаг 3: Создать минимальную настройку для теста

### Точки спавна инструментов
Создать 3 пустых GameObject:
- `ToolSpawnPoint_Heater` (в локации 1)
- `ToolSpawnPoint_Destructor` (в локации 1) 
- `ToolSpawnPoint_Sprayer` (в локации 1)

### Префабы инструментов (упрощённо)
Для быстрого теста можно использовать кубики:
1. Создать GameObject → 3D Object → Cube
2. Масштаб: (0.3, 0.3, 0.3)
3. Добавить компонент `Progression.PickupableTool`
4. Настроить Tool Type
5. Visual Model = сам куб
6. Сохранить как префаб

### Триггер-зоны
1. Создать GameObject
2. Добавить Box Collider, Is Trigger = true
3. Добавить `Progression.ProgressionTriggerZone`
4. Разместить на входе в локацию

## Шаг 4: Настроить первые 2 шага для теста

В `GameProgressionManager` → Steps:

### Шаг 0
- Step Id: `test_pickup_heater`
- Task Text: `Тест: Возьми инструмент`
- Entry Actions (Add):
  - `ShowTaskAction`: Task Text = `Тест: Возьми инструмент`
  - `SpawnToolAction`: Tool Type = Heater, Spawn Point, Pickup Prefab
- Conditions (Add):
  - `ToolPickedCondition`: Tool Type = Heater

### Шаг 1
- Step Id: `test_clean`
- Task Text: `Тест: Очисти локацию`
- Entry Actions (Add):
  - `ShowTaskAction`: Task Text = `Тест: Очисти локацию`
- Conditions (Add):
  - `LocationCleanedCondition`: Location = TrashLocation локации 1

## Шаг 5: Запустить и протестировать

1. **Play**: Должен появиться текст "Тест: Возьми инструмент"
2. **Подойти к кубу**: Нажать E - инструмент подберётся
3. **Нажать 2**: Переключится на Heater
4. **Очистить мусор**: После 100% шаг завершится
5. **Проверка**: Появится "Тест: Очисти локацию"

## Возможные проблемы

### "DirectionArrow не найден"
- Убедитесь, что компонент добавлен на объект с GameHud

### "ToolInventory не найден"
- Компонент должен быть на игроке

### "Инструмент не подбирается"
- Проверьте, что на префабе есть `PickupableTool`
- Убедитесь, что игрок может взаимодействовать (PlayerRaycastInteract работает)

### Инструмент не переключается
- Убедитесь, что в ToolController правильно настроен массив toolTypes
- Проверьте, что ToolInventory находится на том же объекте, что и ToolController, или доступен через FindFirstObjectByType

## Полная настройка

После успешного теста используйте полную инструкцию из `README.md` для настройки всех 10 шагов.
