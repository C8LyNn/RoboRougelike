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
        // W34: a destroyed Rigidbody can be retained by both Dictionary caches
        // even when its Collider exit event did fire. Exercise original callbacks.
        body.Destroyed = true;
        var newBody = new Rigidbody { gameObject = new GameObject() };
        var newPart = new BoxCollider { gameObject = new GameObject(),
                                        attachedRigidbody = newBody };
        Contact(pad, newPart);
        Require(newBody.linearVelocity.y == 10f, "new real source body still launches");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var contacts = (System.Collections.IDictionary)(typeof(MapJumpPad)
            .GetField("contacts", flags)?.GetValue(pad)
            ?? throw new Exception("original contacts cache missing"));
        var launches = (System.Collections.IDictionary)(typeof(MapJumpPad)
            .GetField("lastLaunch", flags)?.GetValue(pad)
            ?? throw new Exception("original lastLaunch cache missing"));
        if (contacts.Count != 1 || launches.Count != 1 ||
            !contacts.Contains(newBody) || !launches.Contains(newBody))
        {
            Console.WriteLine("EXPECTED_DESTROYED_BODY_CACHE_RED: contacts="
                + contacts.Count + ", lastLaunch=" + launches.Count);
            return 24;
        }

        Console.WriteLine("B0121_ORIGINAL_CSHARP_GREEN: disabled+destroyed collider recovery;"
            + " live duplicate suppressed; destroyed body retired from both caches");
        return 0;
    }
}
