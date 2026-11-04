# HUD

HUD сделан отдельным UI Toolkit-модулем. Он не изменяет игровые системы: получает данные через публичные методы или подписку на событие источника.

## Состав

- `GameHud` — отображает задачу, полосы `CLEAN` и `PAINT`, карточку объекта и предупреждение.
- `HudObjectInfo` — текст карточки на игровом объекте.
- `HudObjectInfoDetector` — показывает карточку объекта под прицелом камеры.
- `HudWarningZone` — включает `CRITICAL WARNING`, пока игрок находится вне триггера.
- `GameHud.uxml` и `GameHud.uss` — разметка и стили.

Курсор добавлен в тот же UIDocument и использует существующие `CrosshairController` и `CursorManager`.

## Подключение в сцене

1. Создайте объект HUD с компонентом `UIDocument`.
2. Назначьте ему `Assets/UI/UXML/GameHud.uxml` и существующий `PanelSettings`.
3. Добавьте `GameHud`, `HudObjectInfoDetector`, `CrosshairController`, `CursorManager` и `EdgeGlitchEffect`.
4. В `GameHud` назначьте UIDocument и источник данных (`TrashLocation` в примере ниже).
5. В `HudObjectInfoDetector` назначьте `GameHud`, при необходимости — камеру.
6. В `CrosshairController` назначьте UIDocument.

## Подключение произвольного модуля через event

`TrashLocation` — пример источника состояния: он предоставляет значение `CleanedPercent` и событие `Changed`. HUD подписывается в `OnEnable`, отписывается в `OnDisable` и обновляет только своё представление.

Для нового источника используйте такой же контракт:

```csharp
public float Percent { get; private set; }
public event Action Changed;
```

После изменения значения вызывайте `Changed?.Invoke()`. Адаптер HUD подписывается на событие и вызывает публичный метод HUD, например `hud.SetPaint(source.Percent)`. Игровая система не должна знать о конкретной разметке HUD.

## Тестовая сцена

`Assets/Scenes/HudTest.unity` создаёт игрока, тестовый объект с карточкой, `TrashLocation`, HUD и разрешённую зону. Запустите сцену, наведитесь на куб, затем выйдите за её границы для проверки предупреждения.
