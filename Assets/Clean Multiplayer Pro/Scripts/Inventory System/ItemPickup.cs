#if CMPSETUP_COMPLETE
using System;
using Fusion;
using UnityEngine;

public class ItemPickup : NetworkBehaviour
{
    [SerializeField] private InventoryItem inventoryItem;
    private Rigidbody _rb;
    private bool _isPickedUp;

    private void Awake()
    {
        TryGetComponent(out _rb);
    }

    public void PickUp(NetworkObject networkObject)
    {
        if(_isPickedUp)
            return;
        if (networkObject.StateAuthority != Runner.LocalPlayer)
            return;

        if (!networkObject.CompareTag("Player"))
            return;

        if (inventoryItem == null)
        {
            Debug.LogError("No inventory item found!");
        }

        _isPickedUp = true;
        _rb.isKinematic = true;
        Inventory.AddItemToInventory?.Invoke(inventoryItem);
        DespawnRPC();
    }

    [Rpc(RpcSources.All,RpcTargets.StateAuthority)]
    private void DespawnRPC()
    {
        Runner.Despawn(Object);
    }
}
#endif