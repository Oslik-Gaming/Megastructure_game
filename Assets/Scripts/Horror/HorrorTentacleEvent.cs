using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хоррор-ивент «щупальце в тоннеле».
///
/// Игрок входит в триггер-зону на точке (POINT_HORROR_N) → из точки вылезает большое
/// щупальце (собрано из цилиндров и узкого конуса на конце, материал — как у паразита).
/// Звук — тот же зацикленный звук слизи, что у паразита (AudioSource на щупальце).
/// Одновременно активируется зона опасности на всю локацию → красный баннер в HUD.
///
/// Щупальце застывает «знаком вопроса», кончиком разворачиваясь на игрока.
/// Как только игрок замечает его (смотрит на кончик с расстояния ближе Notice Distance,
/// в пределах угла обзора и с чистой линией взгляда) — щупальце полностью уходит
/// под пол (в тоннель) и исчезает, баннер опасности гаснет.
///
/// Подсвечивается по Q как паразит — красным (см. TrashHighlightController).
/// Ивент одноразовый: после ухода щупальца больше не появляется.
/// </summary>
[DisallowMultipleComponent]
public class HorrorTentacleEvent : MonoBehaviour
{
    private enum State { Idle, Emerging, Showing, Retreating, Done }

    [Header("Ссылки")]
    [Tooltip("Корень щупальца (пивот на уровне пола/тоннеля). В покое утоплен под пол на высоту щупальца.")]
    [SerializeField] private Transform tentacleRoot;

    [Tooltip("Кончик щупальца — точка, на которую «смотрит» игрок и по которой считается дистанция. Пусто — берётся tentacleRoot.")]
    [SerializeField] private Transform tentacleTip;

    [Tooltip("TrashLocation, к которой относится ивент (для подсветки по Q). Пусто — берётся ближайшая на сцене.")]
    [SerializeField] private TrashLocation homeLocation;

    [Tooltip("Зона опасности (HudWarningZone в режиме Warn Inside) на всю локацию. Включается на время ивента — пока щупальце видно, затем выключается и баннер гаснет.")]
    [SerializeField] private HudWarningZone dangerZone;

    [Header("Звуки (как у паразита)")]
    [Tooltip("AudioSource со зацикленным звуком слизи. Пусто — берётся AudioSource на щупальце.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Дополнительный звук вылезания из тоннеля. Пусто — не играет.")]
    [SerializeField] private AudioClip emergeSound;

    [Tooltip("Дополнительный звук ухода под пол. Пусто — не играет.")]
    [SerializeField] private AudioClip retreatSound;

    [Header("Анимация")]
    [Tooltip("На какую высоту щупальце вылезает из тоннеля (скрытая позиция + это значение = рабочая позиция).")]
    [SerializeField, Min(0.1f)] private float emergeHeight = 2.9f;

    [Tooltip("Время вылезания из тоннеля, сек.")]
    [SerializeField, Min(0.1f)] private float emergeDuration = 1.6f;

    [Tooltip("Время полного ухода под пол, сек.")]
    [SerializeField, Min(0.05f)] private float retreatDuration = 0.8f;

    [Tooltip("Скорость разворота кончиком на игрока.")]
    [SerializeField, Min(0.1f)] private float aimTurnSpeed = 3f;

    [Tooltip("Амплитуда покачивания щупальца, градусы.")]
    [SerializeField, Min(0f)] private float swayAmplitude = 4f;

    [Tooltip("Скорость покачивания щупальца.")]
    [SerializeField, Min(0.1f)] private float swaySpeed = 1.5f;

    [Header("Когда щупальце прячется (обнаружение игроком)")]
    [Tooltip("Щупальце прячет себя и гасит баннер, когда игрок СМОТРИТ на его кончик одновременно при двух условиях:\n1) дистанция до кончика <= Notice Distance;\n2) кончик в пределах угла обзора камеры <= Notice Angle\nв течение Look Time To Notice секунд подряд.")]
    [SerializeField, Min(0.5f)] private float noticeDistance = 12f;

    [Tooltip("Угол обзора от направления камеры, в котором щупальце считается видимым, градусы.")]
    [SerializeField, Range(1f, 90f)] private float noticeAngle = 40f;

    [Tooltip("Сколько секунд нужно непрерывно смотреть на щупальце, чтобы оно «заметили».")]
    [SerializeField, Min(0f)] private float lookTimeToNotice = 0.35f;

    [Tooltip("Слои, блокирующие линию взгляда (стены/препятствия). Пусто — линия всегда чистая.")]
    [SerializeField] private LayerMask sightBlockMask = ~0;

    [Tooltip("Анимировать рост МАСШТАБОМ вместо подъёма из-под пола. Включите, если ставите свою модель щупальца — оно будет расти от пивота, как из глубины.")]
    [SerializeField] private bool useScaleGrowth = false;

    [Tooltip("Камера игрока. Пусто — берётся Camera.main, а если она не помечена тегом MainCamera — первая включённая камера на сцене.")]
    [SerializeField] private Camera viewCamera;

    [Tooltip("Отладка: рисовать дистанцию обнаружения и триггер-зону в Scene view.")]
    [SerializeField] private bool debugDraw = false;

    private State state = State.Idle;
    private float timer;
    private float lookTimer;
    private float currentYaw;
    private float loopStartVolume = 1f;
    private Vector3 hiddenLocalPos;
    private Vector3 shownLocalPos;
    private Vector3 initialScale;
    private Transform playerTransform;
    private Camera playerCamera;

    private static readonly List<HorrorTentacleEvent> instances = new();

    /// <summary>Все активные ивенты на сцене (используется подсветкой по Q).</summary>
    public static IReadOnlyList<HorrorTentacleEvent> Instances => instances;

    /// <summary>Локация, к которой привязан ивент (для подсветки по Q).</summary>
    public TrashLocation EventLocation => homeLocation;

    /// <summary>Объект, который подсвечивается по Q (корень щупальца).</summary>
    public GameObject HighlightTarget => tentacleRoot != null ? tentacleRoot.gameObject : null;

    /// <summary>Видно ли щупальце сейчас (для подсветки по Q).</summary>
    public bool HighlightVisible =>
        state == State.Emerging || state == State.Showing || state == State.Retreating;

    /// <summary>Ивент уже отыгран (щупальце спряталось навсегда).</summary>
    public bool IsFinished => state == State.Done;

    private void Awake()
    {
        if (audioSource == null && tentacleRoot != null)
            audioSource = tentacleRoot.GetComponentInChildren<AudioSource>(true);

        if (homeLocation == null)
        {
            float best = float.MaxValue;
            foreach (TrashLocation loc in FindObjectsByType<TrashLocation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                float d = (loc.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; homeLocation = loc; }
            }
        }

        if (tentacleRoot != null)
        {
            hiddenLocalPos = tentacleRoot.localPosition;
            shownLocalPos = hiddenLocalPos + Vector3.up * emergeHeight;
            initialScale = tentacleRoot.localScale;
            currentYaw = transform.eulerAngles.y;

            // В покое щупальца нет вообще — игрок не должен видеть его до входа в зону.
            tentacleRoot.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);
    }

    private void OnDisable() => instances.Remove(this);

    /// <summary>Запустить ивент вручную (запускается HorrorTentacleTrigger из дочерней TriggerZone).</summary>
    public void StartEvent()
    {
        if (state != State.Idle || tentacleRoot == null)
            return;

        state = State.Emerging;
        timer = 0f;
        lookTimer = 0f;
        playerCamera = ResolveCamera();
        playerTransform = FindFirstObjectByType<PlayerMovements>()?.transform;

        // Щупальце появляется «с самых глубин»: включается только сейчас,
        // когда игрок вошёл в зону.
        tentacleRoot.gameObject.SetActive(true);
        if (useScaleGrowth)
        {
            tentacleRoot.localPosition = shownLocalPos;
            tentacleRoot.localScale = initialScale * 0.01f;
        }
        else
            tentacleRoot.localPosition = hiddenLocalPos;

        // Вошли в триггер-зону → активируем опасную зону: баннер появляется сразу.
        if (dangerZone != null)
            dangerZone.Activate();

        if (audioSource != null && audioSource.clip != null)
        {
            loopStartVolume = audioSource.volume;
            audioSource.Play();
        }

        if (emergeSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(emergeSound);
            else AudioSource.PlayClipAtPoint(emergeSound, transform.position);
        }
    }

    /// <summary>
    /// Найти камеру игрока. Camera.main работает только с тегом MainCamera;
    /// если камера не помечена (как DevCamera в этой сцене) — берём первую включённую.
    /// </summary>
    private Camera ResolveCamera()
    {
        if (viewCamera != null)
            return viewCamera;
        if (Camera.main != null)
            return Camera.main;
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (cam.enabled && cam.isActiveAndEnabled)
                return cam;
        return null;
    }

    private void Update()
    {
        if (tentacleRoot == null)
            return;

        switch (state)
        {
            case State.Emerging:
            {
                timer += Time.deltaTime;
                float k = Mathf.Clamp01(timer / emergeDuration);
                if (useScaleGrowth)
                    tentacleRoot.localScale = Vector3.Lerp(initialScale * 0.01f, initialScale, EaseOutCubic(k));
                else
                    tentacleRoot.localPosition = Vector3.Lerp(hiddenLocalPos, shownLocalPos, EaseOutCubic(k));
                if (k >= 1f)
                {
                    state = State.Showing;
                    timer = 0f;
                }
                break;
            }

            case State.Showing:
                AimAtPlayer();
                UpdateNoticeCheck();
                break;

            case State.Retreating:
            {
                timer += Time.deltaTime;
                float r = Mathf.Clamp01(timer / retreatDuration);
                if (useScaleGrowth)
                    tentacleRoot.localScale = Vector3.Lerp(initialScale, initialScale * 0.01f, EaseInCubic(r));
                else
                    tentacleRoot.localPosition = Vector3.Lerp(shownLocalPos, hiddenLocalPos, EaseInCubic(r));
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.volume = loopStartVolume * (1f - r);
                if (r >= 1f)
                    FinishRetreat();
                break;
            }
        }
    }

    /// <summary>Разворачивает щупальце кончиком на игрока + лёгкое живое покачивание.</summary>
    private void AimAtPlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 toPlayer = playerTransform.position - tentacleRoot.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            float targetYaw = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, aimTurnSpeed * Time.deltaTime);
        }

        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;
        float swayZ = Mathf.Sin(Time.time * swaySpeed * 0.6f + 1.7f) * swayAmplitude * 0.6f;
        tentacleRoot.rotation = Quaternion.Euler(swayX, currentYaw, swayZ);
    }

    /// <summary>Проверка: заметил ли игрок щупальце (дистанция + угол обзора + линия взгляда).</summary>
    private void UpdateNoticeCheck()
    {
        if (playerCamera == null)
            playerCamera = ResolveCamera();
        if (playerCamera == null)
            return;

        Vector3 tipPos = GetTipPosition();
        Vector3 toTip = tipPos - playerCamera.transform.position;
        float distance = toTip.magnitude;

        if (distance <= noticeDistance)
        {
            float angle = Vector3.Angle(playerCamera.transform.forward, toTip);
            if (angle <= noticeAngle && HasLineOfSight(playerCamera.transform.position, tipPos, distance))
                lookTimer += Time.deltaTime;
        }

        if (lookTimer >= lookTimeToNotice)
            OnNoticed();
    }

    private Vector3 GetTipPosition()
    {
        if (tentacleTip != null)
            return tentacleTip.position;
        return tentacleRoot != null
            ? tentacleRoot.position + Vector3.up * emergeHeight * 0.85f
            : transform.position;
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to, float distance)
    {
        Vector3 dir = (to - from) / Mathf.Max(0.001f, distance);
        if (Physics.Raycast(from, dir, out RaycastHit hit, Mathf.Max(0.05f, distance - 0.05f),
                sightBlockMask.value, QueryTriggerInteraction.Ignore))
        {
            // Попадание в само щупальце или в игрока линию взгляда не блокирует.
            if (tentacleRoot != null && hit.collider.transform.IsChildOf(tentacleRoot))
                return true;
            if (playerTransform != null && hit.collider.transform.IsChildOf(playerTransform))
                return true;
            return false;
        }
        return true;
    }

    /// <summary>Игрок заметил щупальце: гасим баннер и уводим щупальце под пол.</summary>
    private void OnNoticed()
    {
        if (state != State.Showing)
            return;

        // Игрок увидел щупальце → разоружаем опасную зону: баннер гаснет.
        if (dangerZone != null)
            dangerZone.Deactivate();

        if (retreatSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(retreatSound);
            else AudioSource.PlayClipAtPoint(retreatSound, tentacleRoot.position);
        }

        state = State.Retreating;
        timer = 0f;
    }

    private void FinishRetreat()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = loopStartVolume;
        }
        tentacleRoot.gameObject.SetActive(false);
        if (useScaleGrowth)
            tentacleRoot.localScale = initialScale;
        else
            tentacleRoot.localPosition = hiddenLocalPos;
        state = State.Done;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t) => t * t * t;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (debugDraw)
        {
            Vector3 tip = Application.isPlaying && tentacleTip != null
                ? tentacleTip.position
                : GetTipPosition();
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(tip, noticeDistance);
        }

        // Рамка зоны запуска (теперь это дочерний объект TriggerZone).
        foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.isTrigger || box.transform == (dangerZone != null ? dangerZone.transform : null))
                continue;
            if (box.name != "TriggerZone")
                continue;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
#endif
}
