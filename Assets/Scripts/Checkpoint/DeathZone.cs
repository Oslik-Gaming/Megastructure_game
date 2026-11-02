using UnityEngine;

/// <summary>
/// Зона смерти: при входе игрока — затемнение экрана на мгновение
/// и респавн на последнем активированном чекпоинте.
///
/// Как пользоваться: добавить на объект с BoxCollider (IsTrigger настроится сам),
/// растянуть габариты под опасную зону (пропасть, лава и т.п.). Логики внутри нет —
/// зон можно сделать сколько угодно.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DeathZone : MonoBehaviour
{
    [Header("Отладка")]
    [Tooltip("Писать в консоль при срабатывании")]
    public bool logTrigger = true;

    /// <summary>Вызывается Unity при добавлении компонента в редакторе — сразу настраиваем триггер.</summary>
    private void Reset()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(10f, 2f, 10f);
    }

    private void Awake()
    {
        // Гарантируем, что коллайдер — триггер (даже если объект собирали вручную)
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CheckpointManager.IsPlayerCollider(other)) return;

        if (logTrigger)
            Debug.Log($"[DeathZone] '{name}': игрок погиб, респавн на последнем чекпоинте.", this);

        CheckpointManager.KillPlayer();
    }
}
