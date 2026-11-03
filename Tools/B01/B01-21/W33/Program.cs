using System;
using System.Reflection;
using B01.MapJumpPad001;
using UnityEngine;

// Directly executes the ORIGINAL MapJumpPad.cs C# callback with a deliberately
// stubbed engine, not a recreation of its algorithm or physical Unity G1.
public static class Program
{
    private static readonly MethodInfo Enter = typeof(MapJumpPad).GetMethod(
        "OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Original callback not found");
    private static void Contact(MapJumpPad pad, Collider collider)
        => Enter.Invoke(pad, new object[] { collider });
    private static void Require(bool ok, string label)
    { if (!ok) throw new Exception("SOURCE_METHOD_ASSERT_FAIL: " + label); }

    public static int Main()
    {
        var pad = new MapJumpPad();
        pad.Configure(10f, 0f, 0f); // zero cooldown isolates contact deduplication
        var rigObject = new GameObject();
        var body = new Rigidbody { gameObject = rigObject };
        var oldPart = new BoxCollider { gameObject = rigObject, attachedRigidbody = body };

        Contact(pad, oldPart);
        Require(body.linearVelocity.y == 10f, "initial authentic source callback launch");

        body.linearVelocity = Vector3.zero;
        oldPart.enabled = false; // deliberately omit OnTriggerExit
        var replacement = new BoxCollider { gameObject = new GameObject(),
                                            attachedRigidbody = body };
        Contact(pad, replacement);
        bool recovered = body.linearVelocity.y == 10f;

        // A second live collider of the same Rigidbody must not add an impulse.
        body.linearVelocity = Vector3.zero;
        var extraLive = new BoxCollider { gameObject = new GameObject(),
                                          attachedRigidbody = body };
        Contact(pad, extraLive);
        Require(body.linearVelocity.y == 0f, "live duplicate suppression");

        // Lost exit after actual Unity object destruction is a distinct stale case.
        body.linearVelocity = Vector3.zero;
        replacement.Destroyed = true;
        extraLive.enabled = false;
        var third = new BoxCollider { gameObject = new GameObject(),
                                      attachedRigidbody = body };
        Contact(pad, third);
        bool recoveredAfterDestroy = body.linearVelocity.y == 10f;

        if (!recovered || !recoveredAfterDestroy)
        {
            Console.WriteLine("EXPECTED_STALE_CONTACT_RED: disabled="
                + recovered + ", destroyed=" + recoveredAfterDestroy);
            return 23;
        }
        Console.WriteLine("B0121_ORIGINAL_CSHARP_GREEN: disabled+destroyed recovery;"
            + " duplicate live collider suppressed; three genuine source callbacks");
        return 0;
    }
}
