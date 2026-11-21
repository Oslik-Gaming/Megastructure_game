using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;

public static class VRMode
{
    public const string Desktop = "KeyboardAndMouse";
    public const string XR = "XR";
    public static bool Active => XRGeneralSettings.Instance?.Manager?.activeLoader != null || Simulated;
    public static bool Simulated { get; internal set; }
    public static string BindingGroup => VRPlayerRig.Current != null ? XR : Desktop;
    public static bool GameplayBlocked => VRPlayerRig.Current != null && (Time.timeScale <= 0f || VRPlayerRig.Current.Transitioning);

    public static PlayerInputActions CreateInput()
    {
        return new PlayerInputActions { bindingMask = InputBinding.MaskByGroup(BindingGroup) };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Simulated = false;
}
