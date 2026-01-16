#if CMPSETUP_COMPLETE
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkPickUpSpawner : NetworkBehaviour
{
    [SerializeField] private List<InventoryItem> allInventoryItems = new List<InventoryItem>();
    private readonly Dictionary<int, InventoryItem> _playerInventory = new Dictionary<int, InventoryItem>();

    private void Awake()
    {
        SetPlayerInventory();
    }

    private void SetPlayerInventory()
    {
        foreach (var item in allInventoryItems)
        {
            _playerInventory.Add(item.id, item);
        }
    }

    [Rpc]
    public void SpawnPickUpsRPC(int id, Vector3 position)
    {
        if (!Runner.IsSharedModeMasterClient) //Master Client Should Spawn all Pickups 
            return;
        if (!_playerInventory.TryGetValue(id, out var inventoryItem))
        {
            Debug.LogError($"Inventory with an id : {id} Item not found.");
            return;
        }

        Runner.Spawn(inventoryItem.prefab, position, Quaternion.identity);
    }
}
#endif