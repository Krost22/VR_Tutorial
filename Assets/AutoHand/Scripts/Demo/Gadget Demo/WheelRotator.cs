using UnityEngine;
using Autohand;

namespace Autohand.Demo
{
    public class WheelRotator : PhysicsGadgetHingeAngleReader
    {
        public Rigidbody moveRigidbody;
        public Vector3 angle;
        public bool useLocal = false;

        void FixedUpdate()
        {
            float input = GetValue();
            if (Mathf.Abs(input) > 0.01f)
            {
                Quaternion deltaRotation = Quaternion.Euler(angle * Time.fixedDeltaTime * input);

                if (useLocal)
                    moveRigidbody.MoveRotation(moveRigidbody.rotation * moveRigidbody.transform.localRotation * deltaRotation);
                else
                    moveRigidbody.MoveRotation(moveRigidbody.rotation * deltaRotation);
            }
        }
    }
}
