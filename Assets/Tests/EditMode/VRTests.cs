using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public sealed class VRGeometryTests
{
    // Gameplay lives in Assembly-CSharp, which a test asmdef cannot reference directly.
    private static readonly Type MathType = Type.GetType("VRMath, Assembly-CSharp", true);
    private static readonly Func<Vector2, Vector3, Vector3, Vector3, Vector3> Move =
        (Func<Vector2, Vector3, Vector3, Vector3, Vector3>)Delegate.CreateDelegate(typeof(Func<Vector2, Vector3, Vector3, Vector3, Vector3>), MathType.GetMethod("Move"));
    private static readonly Func<Vector3, Vector3, Quaternion, Vector3> Rotate =
        (Func<Vector3, Vector3, Quaternion, Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3, Vector3, Quaternion, Vector3>), MathType.GetMethod("RotatePoint"));

    [TestCase(0f)]
    [TestCase(90f)]
    [TestCase(180f)]
    public void MotionIsHeadRelativeAndTangent(float roll)
    {
        var gravityFrame = Quaternion.AngleAxis(roll, Vector3.forward);
        var up = gravityFrame * Vector3.up;
        var head = gravityFrame * Quaternion.Euler(-35, 31, 0);
        var move = Move(new Vector2(0, 0.4f), head * Vector3.forward, head * Vector3.right, up);
        Assert.That(Vector3.Dot(move, up), Is.EqualTo(0).Within(0.0001f));
        Assert.That(move.magnitude, Is.EqualTo(0.4f).Within(0.0001f));
        Assert.That(Vector3.Angle(move, gravityFrame * Quaternion.Euler(0, 31, 0) * Vector3.forward), Is.LessThan(0.05f));
    }

    [Test]
    public void LookingStraightUpStillAllowsMotion()
    {
        Assert.That(Move(Vector2.up, Vector3.up, Vector3.right, Vector3.up), Is.EqualTo(Vector3.forward));
    }

    [TestCase(45f)]
    [TestCase(180f)]
    public void TurningAnOffsetRigPreservesWorldHeadPosition(float angle)
    {
        Vector3 origin = new(3, 0, 6);
        var rotation = Quaternion.Euler(8, 63, 17);
        Vector3 localHead = new(0.35f, 1.7f, 0.2f);
        Vector3 head = origin + rotation * localHead;
        var delta = Quaternion.AngleAxis(angle, rotation * Vector3.up);
        Assert.That(Vector3.Distance(head, Rotate(origin, head, delta) + delta * rotation * localHead), Is.LessThan(0.0001f));
    }
}

public sealed class VRBindingTests : InputTestFixture
{
    private const string AssetPath = "Assets/PlayerInputActions.inputactions";

    [Test]
    public void XRUsesHandedBindingsAndIgnoresKeyboard()
    {
        InputSystem.RegisterLayout<XRSimulatedController>();
        var left = InputSystem.AddDevice<XRSimulatedController>();
        var right = InputSystem.AddDevice<XRSimulatedController>();
        InputSystem.SetDeviceUsage(left, CommonUsages.LeftHand);
        InputSystem.SetDeviceUsage(right, CommonUsages.RightHand);
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var asset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath));
        try
        {
            asset.bindingMask = InputBinding.MaskByGroup("XR");
            asset.Enable();
            Set(left.primary2DAxis, new Vector2(0.3f, 0.8f));
            Set(right.primary2DAxis, new Vector2(-0.8f, 0f));
            Assert.That(asset["Movement"].ReadValue<Vector2>().y, Is.GreaterThan(0.5f));
            Assert.That(asset["Turn"].ReadValue<Vector2>().x, Is.LessThan(-0.5f));
            Press(keyboard.eKey);
            Assert.That(asset["Interact"].IsPressed(), Is.False);
            Press(right.primaryButton);
            Assert.That(asset["Interact"].WasPerformedThisFrame(), Is.True);
            Press(right.secondaryButton);
            Assert.That(asset["HighlightTrash"].WasPerformedThisFrame(), Is.True);
            Press(left.primaryButton);
            Assert.That(asset["NextTool"].WasPerformedThisFrame(), Is.True);
            Press(left.primary2DAxisClick);
            Assert.That(asset["Sprint"].WasPerformedThisFrame(), Is.True);
            Press(left.triggerButton);
            Assert.That(asset["ToolSecondary"].IsPressed(), Is.True);
            Assert.That(asset["ToolPrimary"].IsPressed(), Is.False);
            Press(right.triggerButton);
            Assert.That(asset["ToolPrimary"].WasPerformedThisFrame(), Is.True);
            Press(left.menuButton);
            Assert.That(asset["PauseMenu"].WasPerformedThisFrame(), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(asset); }
    }

    [Test]
    public void DesktopBindingsStillReadKeyboardAndMouse()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var asset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath));
        try
        {
            asset.bindingMask = InputBinding.MaskByGroup("KeyboardAndMouse");
            asset.Enable();
            Press(keyboard.wKey);
            Assert.That(asset["Movement"].ReadValue<Vector2>(), Is.EqualTo(Vector2.up));
            Press(mouse.leftButton);
            Assert.That(asset["ToolPrimary"].WasPerformedThisFrame(), Is.True);
            Press(keyboard.escapeKey);
            Assert.That(asset["PauseMenu"].WasPerformedThisFrame(), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(asset); }
    }
}
