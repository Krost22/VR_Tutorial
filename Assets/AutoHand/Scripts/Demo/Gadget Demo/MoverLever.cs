//using UnityEngine;
//using Autohand;

//namespace Autohand.Demo{
//    public class MoverLever : PhysicsGadgetHingeAngleReader{
//        public Transform move;
//        public Vector3 axis;
//        public float speed = 1;

//        void Update(){
//            if(Mathf.Abs(GetValue()) > 0.1f)
//                move.position = Vector3.MoveTowards(move.position, move.position-axis, Time.deltaTime*speed*(GetValue()));
//        }
//    }
//}


using UnityEngine;
using Autohand;

namespace Autohand.Demo
{
    public class MoverLever : PhysicsGadgetHingeAngleReader
    {
        public Rigidbody moveRigidbody;  // Referencia al Rigidbody del objeto a mover
        public Vector3 axis = Vector3.forward;
        public float speed = 1;

        void FixedUpdate()
        {
            if (Mathf.Abs(GetValue()) > 0.1f)
            {
                // Mover en la dirección local del Rigidbody
                Vector3 localDirection = moveRigidbody.transform.TransformDirection(axis.normalized);
                Vector3 movement = localDirection * speed * GetValue() * Time.fixedDeltaTime;

                moveRigidbody.MovePosition(moveRigidbody.position + movement);
            }
        }
    }
}