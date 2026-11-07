# Система прогрессии игры

Система управления игровым прогрессом, реализованная согласно `plans/game-state-machine-plan.md`.

## 🚀 Быстрый старт

1. **Прочитайте** `Assets/Docs/progression-inspector-guide.md` — решение проблемы с Inspector
2. **Добавьте** компонент `GameProgressionManager` на пустой GameObject в сцене
3. **Создайте** компонент `ToolInventory` на том же GameObject
4. **Настройте** 10 шагов прогрессии в Inspector согласно плану
5. **Запустите** игру

## 📁 Структура файлов

```
Assets/Scripts/Progression/
├── Core/
│   ├── GameProgressionManager.cs      # Главный менеджер
│   ├── ProgressionStep.cs             # Шаг прогрессии
│   ├── IStepAction.cs                 # Интерфейс действий
│   └── IStepCondition.cs              # Интерфейс условий
├── Actions/                            # 8 типов действий
│   ├── ShowTaskAction.cs
│   ├── SpawnToolAction.cs
│   ├── SwitchLocationTrackingAction.cs
│   ├── ShowDirectionArrowAction.cs
│   ├── HideDirectionArrowAction.cs
│   ├── OpenDoorAction.cs
│   ├── PlayCutsceneAction.cs
│   └── LoadSceneAction.cs
├── Conditions/                         # 4 типа условий
│   ├── ToolPickedCondition.cs
│   ├── LocationCleanedCondition.cs
│   ├── ObjectDestroyedCondition.cs
│   └── ZoneEnteredCondition.cs
├── Components/                         # Вспомогательные компоненты
│   ├── ToolInventory.cs
│   ├── PickupableTool.cs
│   ├── DirectionArrow.cs
│   ├── Door.cs
│   ├── VideoCutscenePlayer.cs
│   ├── ProgressionTriggerZone.cs
│   └── ToolType.cs
└── Editor/
    └── ProgressionStepDrawer.cs       # Кастомный редактор Inspector
```

## 📚 Документация

- **[progression-inspector-guide.md](Assets/Docs/progression-inspector-guide.md)** — как работать с Inspector
- **[game-state-machine-plan.md](plans/game-state-machine-plan.md)** — оригинальный план системы

## 🎮 10 шагов прогрессии

| # | Описание | Entry Actions | Условия завершения |
|---|----------|---------------|-------------------|
| 0 | Подобрать первый мусор | ShowTask | ObjectDestroyed |
| 1 | Подобрать гравитационную пушку | SpawnTool, ShowArrow | ToolPicked |
| 2 | Очистить первую локацию | SwitchLocation | LocationCleaned |
| 3 | Войти во вторую локацию | ShowArrow | ZoneEntered |
| 4 | Подобрать нагреватель | SpawnTool, ShowArrow | ToolPicked |
| 5 | Очистить вторую локацию | SwitchLocation, HideArrow | LocationCleaned |
| 6 | Войти в третью локацию | ShowArrow | ZoneEntered |
| 7 | Подобрать деструктор | SpawnTool, ShowArrow | ToolPicked |
| 8 | Разрушить большой камень | HideArrow | ObjectDestroyed |
| 9 | Очистить третью локацию | SwitchLocation, OpenDoor | LocationCleaned |
| 10 | Финальная катсцена | PlayCutscene | (авто) |

## ⚙️ Ключевые возможности

✅ **Сохранение/загрузка** — состояние автоматически сохраняется через `GameProgress`  
✅ **Идемпотентность** — корректная работа при повторных вызовах `Restore()`  
✅ **Динамическая разблокировка** инструментов через `ToolInventory`  
✅ **Восстановление состояния** после смерти/перезагрузки сцены  
✅ **Расширяемость** — легко добавить новые действия/условия

## 🔧 Интеграция с существующими системами

### GameProgress
Добавлены методы:
- `SaveProgressionStep(int)` / `LoadProgressionStep()` — текущий шаг
- `ResetProgressionStep()` — сброс при "Новой игре"

### ToolController
Обновлён для поддержки `ToolInventory`:
- Проверка разблокированных инструментов
- Автоматический выбор первого доступного инструмента
- Подписка на событие `ToolInventory.ToolUnlocked`

### MainMenuController
Обновлён для сброса прогрессии:
- При "Новой игре" вызывается `GameProgress.ResetProgress()`

## 🐛 Известные проблемы

### Inspector не отображает действия/условия
**Решение:** Убедитесь, что папка `Assets/Scripts/Progression/Editor` существует и содержит `ProgressionStepDrawer.cs`. Перезапустите Unity.

### Инструменты не разблокируются
**Проверьте:**
1. `ToolInventory` добавлен на сцену
2. `ToolController` находит `ToolInventory` через `FindFirstObjectByType`
3. `PickupableTool` корректно указан `toolType`

### Прогресс не сохраняется
**Проверьте:**
1. `GameProgressionManager` вызывает `GameProgress.SaveProgressionStep()` при смене шага
2. PlayerPrefs работает (проверьте логи)

## 🚦 Тестирование

### Быстрый тест всей системы
```csharp
// В консоли Unity
GameProgress.ResetProgress();  // Сброс
// Запустить игру и пройти первые 2-3 шага
```

### Тест отдельных компонентов
```csharp
// ToolInventory
var inv = FindFirstObjectByType<ToolInventory>();
inv.UnlockTool(ToolType.Heater);
Debug.Log(inv.HasTool(ToolType.Heater));  // true

// GameProgressionManager
var mgr = FindFirstObjectByType<GameProgressionManager>();
Debug.Log(mgr.CurrentStepIndex);  // текущий шаг
```

## 📝 Примеры настройки

См. подробные примеры в `progression-inspector-guide.md`.

## 🤝 Расширение системы

### Добавить новое действие
1. Создать класс, реализующий `IStepAction`
2. Пометить `[Serializable]`
3. Добавить тип в `ProgressionStepDrawer.GetActionTypes()`

### Добавить новое условие
1. Создать класс, реализующий `IStepCondition`
2. Пометить `[Serializable]`
3. Добавить тип в `ProgressionStepDrawer.GetConditionTypes()`

---

Система готова к использованию! При возникновении проблем обратитесь к `progression-inspector-guide.md`.
