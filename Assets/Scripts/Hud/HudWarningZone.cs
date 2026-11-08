using UnityEngine;

/// <summary>
/// Зона, управляющая красным баннером опасности в HUD.
///
/// Два режима:
///
/// 1) Обычный (Warn Inside = false, старое поведение): баннер показывается,
///    пока игрок ВНЕ зоны — используется как граница локации.
///
/// 2) Хоррор-режим (Warn Inside = true): зона сама по себе НИЧЕГО не показывает.
///    Её «вооружает» хоррор-ивент: HorrorTentacleEvent вызывает Activate(),
///    когда щупальце вылезло, и Deactivate(), когда оно спряталось.
///    Пока зона вооружена и игрок внутри её бокса — баннер виден.
///    Вошёл в TriggerZone щупальца → Activate → баннер. Никак иначе.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class HudWarningZone : MonoBehaviour
{
    [SerializeField] private GameHud hud;
    public void Configure(GameHud gameHud) => hud = gameHud;

    [Header("Режим")]
    [Tooltip("ВКЛ: баннер только пока зона «вооружена» хоррор-ивентом и игрок внутри зоны. ВЫКЛ: как раньше — баннер, пока игрок ВНЕ зоны.")]
    [SerializeField] private bool warnInside = false;

    [Tooltip("Текст баннера. Пусто — стандартный CRITICAL WARNING.")]
    [SerializeField] private string warningText;

    private bool playerInside;
    private bool armed;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (hud == null)
            hud = FindFirstObjectByType<GameHud>();
    }

    /// <summary>
    /// Вооружить зону (вызывает хоррор-ивент при вылезании щупальца).
    /// Если игрок уже внутри зоны — баннер покажется сразу.
    /// </summary>
    public void Activate()
    {
        armed = true;
        // Компонент должен быть активен, чтобы триггеры входа/выхода работали.
        if (!enabled)
        {
            enabled = true;
            if (playerInside) // не было Awake/OnEnable — проверим игрока прямо сейчас
                playerInside = ContainsPlayer();
        }
        if (hud == null)
            hud = FindFirstObjectByType<GameHud>();
        if (playerInside)
            Show();
    }

    /// <summary>
    /// Разоружить зону (вызывает хоррор-ивент, когда щупальце спряталось).
    /// Баннер гаснет.
    /// </summary>
    public void Deactivate()
    {
        armed = false;
        if (hud != null)
            hud.SetOutsideZone(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;
        playerInside = true;
        if (warnInside)
        {
            if (armed)
                Show();
        }
        else
            hud?.SetOutsideZone(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;
        playerInside = false;
        if (warnInside)
        {
            if (armed)
                hud?.SetOutsideZone(false);
        }
        else
            hud?.SetOutsideZone(true);
    }

    private void Show() => hud?.SetOutsideZone(true, warningText);

    /// <summary>Стоит ли игрок внутри бокса зоны прямо сейчас.</summary>
    private bool ContainsPlayer()
    {
        if (GetComponent<Collider>() is not BoxCollider box)
            return false;
        Transform t = box.transform;
        Vector3 worldCenter = t.TransformPoint(box.center);
        Vector3 half = Vector3.Scale(box.size, t.lossyScale);
        foreach (Collider c in Physics.OverlapBox(worldCenter, half, t.rotation, ~0, QueryTriggerInteraction.Collide))
        {
            if (IsPlayer(c))
                return true;
        }
        return false;
    }

    private static bool IsPlayer(Collider other) => other.GetComponentInParent<PlayerMovements>() != null;
}
