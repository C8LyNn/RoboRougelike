using UnityEngine;

namespace B01.CHL003
{
    /// <summary>
    /// Unity Collision receipts for the real Rigidbody actor; not triggers,
    /// guesses from obstacles' names, or synthetic contact counts.
    /// Installs only on the CHL demo's active Rigidbody root.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CHL003ContactMonitor : MonoBehaviour
    {
        private CHL003CourseController controller;
        public void Configure(CHL003CourseController owner) => controller=owner;
        private void OnCollisionEnter(Collision collision)
        {
            if(controller!=null)controller.RecordContact(collision,true);
        }
        private void OnCollisionStay(Collision collision)
        {
            if(controller!=null)controller.RecordContact(collision,false);
        }
    }
}
