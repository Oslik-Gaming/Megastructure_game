# Быстрая справка: Настройка Inspector

## 🔧 Первый запуск

1. **Создайте пустой GameObject** (ПКМ → Create Empty)
2. **Переименуйте** в "ProgressionManager"
3. **Добавьте компоненты:**
   - Add Component → Game Progression Manager
   - Add Component → Tool Inventory

## 📝 Настройка шагов

### Структура шага в Inspector:

```
▼ Step 0
  ├─ Step Id: "step_0_first_trash"
  ├─ Task Text: "Подобрать первый мусор"
  ├─▼ Entry Actions (Size: 1)
  │  └─▼ Element 0: ShowTaskAction
  │     └─ Game Hud: [GameHud reference]
  │     [Delete]
  │  [Add Entry Actions ▼]
  └─▼ Conditions (Size: 1)
     └─▼ Element 0: ObjectDestroyedCondition
        └─ Target: [DamageableTrashItem]
        [Delete]
     [Add Conditions ▼]
```

## ➕ Добавление действия

1. Раскройте секцию **Entry Actions**
2. Нажмите **[Add Entry Actions]**
3. В выпадающем меню выберите тип:
   ```
   ShowTaskAction          — показать задание
   SpawnToolAction         — создать инструмент
   SwitchLocationTrackingAction — переключить локацию
   ShowDirectionArrowAction — показать стрелку
   HideDirectionArrowAction — скрыть стрелку
   OpenDoorAction          — открыть дверь
   PlayCutsceneAction      — запустить видео
   LoadSceneAction         — загрузить сцену
   ```

## ✅ Добавление условия

1. Раскройте секцию **Conditions**
2. Нажмите **[Add Conditions]**
3. В выпадающем меню выберите тип:
   ```
   ToolPickedCondition       — инструмент подобран
   LocationCleanedCondition  — локация очищена (100%)
   ObjectDestroyedCondition  — объект уничтожен
   ZoneEnteredCondition      — игрок вошёл в зону
   ```

## 🎯 Пример: Шаг 2 (Очистить первую локацию)

```
Step Id: step_2_clean_location_1
Task Text: Очистить первую локацию

Entry Actions:
  ├─ SwitchLocationTrackingAction
  │  └─ New Location: [TrashLocation_1]
  └─ HideDirectionArrowAction
     (нет параметров)

Conditions:
  └─ LocationCleanedCondition
     └─ Location: [TrashLocation_1]
```

## 🚫 Удаление элемента

Нажмите кнопку **[Delete]** справа от действия/условия.

## 🔄 Изменение порядка

Пока не поддерживается визуально — используйте Delete + Add заново.

## ⚠️ Типичные ошибки

| Проблема | Решение |
|----------|---------|
| Плашки "Element 0" без полей | Перезапустите Unity |
| Кнопки Add не работают | Проверьте наличие папки `Editor/` |
| Ссылки сбрасываются | Используйте Prefab варианты |

## 📚 Дополнительно

- Полная документация: `progression-system-overview.md`
- Примеры всех 10 шагов: `ProgressionSetupExample.cs`
- Детали Inspector: `progression-inspector-guide.md`

---

**Совет:** Начните с настройки первых 2-3 шагов и протестируйте игру, прежде чем настраивать все 10.
