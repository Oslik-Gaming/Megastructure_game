using UnityEngine;

[DisallowMultipleComponent]
public sealed class HudObjectInfo : MonoBehaviour
{
    [SerializeField] private string objectTitle = "UNKNOWN OBJECT";
    [SerializeField, TextArea(2, 6)] private string description = "NO DATA";

    public string Title => objectTitle;
    public string Description => description;
}
