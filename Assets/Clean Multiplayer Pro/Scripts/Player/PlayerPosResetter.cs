#if CMPSETUP_COMPLETE
using System;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AvocadoShark
{
    public class PlayerPosResetter : NetworkBehaviour
    {
        public float minYValue = -10f;

        private void LateUpdate()
        {
            if (!HasStateAuthority)
                return;
            if (transform.position.y < minYValue)
            {
                ResetPlayerPosition();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!HasStateAuthority)
                return;
            if (!hit.gameObject.TryGetComponent(out ItemPickup item))
                return;

            item.PickUp(Object);
        }

        private void ResetPlayerPosition()
        {
            transform.position = FusionConnection.Instance.UseCustomLocation
                ? FusionConnection.Instance.CustomLocation
                : new Vector3(Random.Range(-7.6f, 14.2f), 0, Random.Range(-31.48f, -41.22f));
        }
    }
}
#endif