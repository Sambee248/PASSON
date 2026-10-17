using NUnit.Framework;
using UnityEngine;

public sealed class StageGateTests
{
    [Test]
    public void Opening_the_gate_disables_its_blocking_collider()
    {
        var gateObject = new GameObject("Stage gate");
        var blocker = gateObject.AddComponent<BoxCollider>();
        var gate = gateObject.AddComponent<StageGate>();

        try
        {
            gate.OpenForTest();

            Assert.That(gate.IsOpen, Is.True);
            Assert.That(blocker.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(gateObject);
        }
    }
}
