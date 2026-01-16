#if CMPSETUP_COMPLETE
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class Inventory : MonoBehaviour
{
    public static Action<InventoryItem> AddItemToInventory;
    public static Action<InventoryItem> RemoveItemFromInventory;
    [SerializeField] private List<InventoryItem> inventoryItems = new List<InventoryItem>();
    [SerializeField] private NetworkPickUpSpawner networkPickUpSpawner;
    private readonly Dictionary<int, InventoryItem> _inventoryItemsDict = new Dictionary<int, InventoryItem>();
    private Dictionary<int, int> _inventory = new Dictionary<int, int>();
    private const string Key = "InventoryData";

    private void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        AddItemToInventory += AddItem;   
        RemoveItemFromInventory += RemoveItem; 
    }

    private void OnDisable()
    {
        AddItemToInventory -= AddItem;   
        RemoveItemFromInventory -= RemoveItem;
    }

    private void OnDestroy()
    {
        SaveInventoryData();
    }

    private void Init()
    {
        LoadInventoryData();
        foreach (var item in inventoryItems)
        {
            _inventoryItemsDict.TryAdd(item.id, item);
        }

    }
    private void AddItem(InventoryItem inventoryItem)
    {
        if (!_inventory.TryAdd(inventoryItem.id, 1))
        {
            _inventory[inventoryItem.id]++;
        }
    }

    private void RemoveItem(InventoryItem inventoryItem)
    {
        if (!_inventory.ContainsKey(inventoryItem.id))
        {
            Debug.Log($"Item cannot be removed because Item with Id {inventoryItem.id} doesn't exist");
            return;
        }

        _inventory[inventoryItem.id]--;

        if (_inventory[inventoryItem.id] <= 0)
        { 
            _inventory.Remove(inventoryItem.id);
        }
    }
    public Dictionary<InventoryItem, int> GetAllInventoryItems()
    {
        return _inventory.ToDictionary(item => _inventoryItemsDict[item.Key], item => item.Value);
    }

    public void SpawnNetworkPickUp(int id,Vector3 position)
    {
        networkPickUpSpawner.SpawnPickUpsRPC(id, position);
    }

    private void SaveInventoryData()
    {
        var json = JsonUtility.ToJson(new InventoryData(_inventory));  
        PlayerPrefs.SetString(Key, json);
    }
    
    private void LoadInventoryData()
    {
        if(!PlayerPrefs.HasKey(Key))
            return;
        var json = PlayerPrefs.GetString(Key);
        _inventory = JsonUtility.FromJson<InventoryData>(json);
    }
}

[Serializable]
public class InventoryData
{
    [Serializable]
    public class ItemData
    {
        public int id;
        public int quantity;

        public ItemData(int iKey, int iValue)
        {
            id = iKey;
            quantity = iValue;
        }
    }
    public List<ItemData> inventory = new List<ItemData>();

    public InventoryData(Dictionary<int, int> inventory)
    {
        foreach (var i in inventory)
        {
            this.inventory.Add(new ItemData(i.Key, i.Value));
        }
    }

    public static implicit operator Dictionary<int, int>(InventoryData inventoryData)
    {
        return inventoryData.inventory.ToDictionary(i => i.id, i => i.quantity);
    }
}
#endif