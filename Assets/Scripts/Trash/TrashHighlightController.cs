using System.Collections.Generic;
using UnityEngine;
using Progression;

public sealed class TrashHighlightController : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnPlayer()
    {
        foreach (var player in FindObjectsByType<PlayerMovements>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (player.GetComponent<TrashHighlightController>() == null)
                player.gameObject.AddComponent<TrashHighlightController>();
    }

    [SerializeField] private GameHud hud;
    [SerializeField] private DirectionArrow directionArrow;
    [SerializeField] private Transform distanceOrigin;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.3f;
    [SerializeField] private Color trashColor = new(0.2f, 1f, 0.3f);
    [SerializeField] private Color rockColor = new(1f, 0.55f, 0.05f);
    [SerializeField] private Color parasiteColor = new(1f, 0.1f, 0.1f);
    private PlayerInputActions input;
    private bool held;
    private readonly Dictionary<TrashItem, TrashHighlightable> highlights = new();

    private void Awake()
    {
        if (hud == null) hud = FindFirstObjectByType<GameHud>();
        if (directionArrow == null) directionArrow = FindFirstObjectByType<DirectionArrow>();
        if (distanceOrigin == null) distanceOrigin = transform;
        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (input == null) input = new PlayerInputActions();
        input.Player.HighlightTrash.started += OnStarted;
        input.Player.HighlightTrash.canceled += OnCanceled;
        input.Player.Enable();
        if (hud != null) { hud.LocationChanged += OnLocationChanged; }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.Player.HighlightTrash.started -= OnStarted;
            input.Player.HighlightTrash.canceled -= OnCanceled;
            input.Player.Disable();
            input.Dispose();
            input = null;
        }
        if (hud != null) hud.LocationChanged -= OnLocationChanged;
        StopHighlight();
    }

    private void OnStarted(UnityEngine.InputSystem.InputAction.CallbackContext _) { if (!PauseMenuController.GamePaused) { held = true; Refresh(); } }
    private void OnCanceled(UnityEngine.InputSystem.InputAction.CallbackContext _) { StopHighlight(); }
    private void OnLocationChanged(TrashLocation _) { if (held) Refresh(); }

    private void Update()
    {
        if (!held || PauseMenuController.GamePaused) return;
        Refresh();
    }

    private void Refresh()
    {
        var location = hud?.CurrentLocation;
        var current = new HashSet<TrashItem>(location?.ActiveItems ?? System.Array.Empty<TrashItem>());
        foreach (var item in new List<TrashItem>(highlights.Keys))
        {
            if (!current.Contains(item) || item == null || item.IsRemoved)
            {
                highlights[item].SetHighlighted(false);
                highlights.Remove(item);
            }
        }
        foreach (var item in current)
        {
            if (item == null || item.IsRemoved) continue;
            if (!highlights.TryGetValue(item, out var highlight))
            {
                highlight = item.GetComponent<TrashHighlightable>() ?? item.gameObject.AddComponent<TrashHighlightable>();
                highlight.Initialize(GetColor(item), fadeOutDuration);
                highlights.Add(item, highlight);
            }
            highlight.SetHighlighted(true);
        }
        TrashItem nearest = FindNearest(current);
        if (directionArrow != null)
        {
            if (nearest != null) directionArrow.ShowOverride(nearest.transform);
            else directionArrow.HideOverride();
        }
    }

    private TrashItem FindNearest(HashSet<TrashItem> items)
    {
        TrashItem nearest = null;
        float best = float.PositiveInfinity;
        foreach (var item in items)
        {
            if (item == null || item.IsRemoved) continue;
            float distance = (item.transform.position - distanceOrigin.position).sqrMagnitude;
            if (distance < best) { best = distance; nearest = item; }
        }
        return nearest;
    }

    private Color GetColor(TrashItem item) => item switch
    {
        Parasite => parasiteColor,
        BigRock => rockColor,
        _ => trashColor
    };

    private void StopHighlight()
    {
        held = false;
        foreach (var highlight in highlights.Values)
            if (highlight != null) highlight.SetHighlighted(false);
        highlights.Clear();
        directionArrow?.HideOverride();
    }
}
