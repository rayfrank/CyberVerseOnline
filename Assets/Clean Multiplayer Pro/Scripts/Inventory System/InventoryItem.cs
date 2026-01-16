#if CMPSETUP_COMPLETE
using System;
using Fusion;
using UnityEngine;

[Serializable,CreateAssetMenu(menuName = "ScriptableObject/Inventory System/Item")]
public class InventoryItem : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public string description;
    public NetworkObject prefab;
}
#endif