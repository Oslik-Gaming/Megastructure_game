using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class VRPivotPlayTests
{
    [UnityTest]
    public IEnumerator OffsetHeadInScaledRobotHierarchyDoesNotTranslateOnSnap()
    {
        var robot = new GameObject("Scaled robot");
        try
        {
            robot.transform.localScale = Vector3.one * 0.06f;
            var origin = new GameObject("XR Origin").transform;
            origin.SetParent(robot.transform, false);
            origin.localScale = Vector3.one / 0.06f;
            var head = new GameObject("Tracked head").transform;
            head.SetParent(origin, false);
            head.localPosition = new Vector3(0.3f, 1.65f, 0.45f);
            var before = head.position;
            var delta = Quaternion.AngleAxis(45f, robot.transform.up);
            var math = Type.GetType("VRMath, Assembly-CSharp", true);
            var rotated = (Vector3)math.GetMethod("RotatePoint").Invoke(null, new object[] { origin.position, before, delta });
            origin.SetPositionAndRotation(rotated, delta * origin.rotation);
            yield return null;
            Assert.That(Vector3.Distance(head.position, before), Is.LessThan(0.0001f));
        }
        finally { UnityEngine.Object.Destroy(robot); }
    }
}
