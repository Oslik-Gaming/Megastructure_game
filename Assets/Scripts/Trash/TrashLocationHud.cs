using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TrashLocationHud : MonoBehaviour
{
    [SerializeField] private TrashLocation location;
    [SerializeField] private TextMeshProUGUI percentText;
    [Tooltip("Показывать оставшийся мусор: 100% -> 0%. Иначе отображается прогресс уборки.")]
    [SerializeField] private bool showRemainingTrash = true;
    [SerializeField] private string format = "{0:0}%";

    public void Configure(TrashLocation trashLocation, TextMeshProUGUI text, bool displayRemainingTrash)
    {
        if (location != null)
            location.Changed -= Refresh;

        location = trashLocation;
        percentText = text;
        showRemainingTrash = displayRemainingTrash;

        if (isActiveAndEnabled && location != null)
            location.Changed += Refresh;

        Refresh();
    }

    private void OnEnable()
    {
        if (location == null)
            return;

        location.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (location != null)
            location.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (location == null || percentText == null)
            return;

        float value = showRemainingTrash ? location.RemainingPercent : location.CleanedPercent;
        percentText.text = string.Format(format, value);
    }
}
