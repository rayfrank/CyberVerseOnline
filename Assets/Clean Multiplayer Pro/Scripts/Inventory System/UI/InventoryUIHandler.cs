#if CMPSETUP_COMPLETE
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryUIHandler : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Transform scrollViewContent;
    [SerializeField] private GameObject uiParent;
    [SerializeField] private TextMeshProUGUI inventoryToggleText;
    [SerializeField] private ItemUI itemUIPrefab;

    [SerializeField] private List<GameObject> overlappingUIObjects;

    [SerializeField] private UIPool<ItemUI> _spawnedItemUIObjects;
    [SerializeField] private GameObject errorMessage;
    private bool _isOpen = false;

    private void Awake()
    {
        _spawnedItemUIObjects = new UIPool<ItemUI>(itemUIPrefab,scrollViewContent);
    }
    private void OnEnable()
    {
        Inventory.AddItemToInventory += OnItemAddOrRemove;   
        Inventory.RemoveItemFromInventory += OnItemAddOrRemove; 
    }

    private void OnDisable()
    {
        Inventory.AddItemToInventory -= OnItemAddOrRemove;
        Inventory.RemoveItemFromInventory -= OnItemAddOrRemove;
    }
    public void ToggleInventory()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
            ShowInventoryUI();
        else
            HideInventory();

        SetOverlappingUIObjectsState(!_isOpen);
    }

    private void HideInventory()
    {
        _spawnedItemUIObjects.ReturnAllObjects();
        uiParent.SetActive(false);
        inventoryToggleText.SetText("Inventory");
    }

    private void ShowInventoryUI()
    {
        var allItems = inventory.GetAllInventoryItems();
        foreach (var item in allItems)
        {
            var itemUI = _spawnedItemUIObjects.RentObject();
            itemUI.SetValue(
                new ValueTuple<Canvas, InventoryUIHandler,Inventory, InventoryItem, int>(canvas,this, inventory, item.Key, item.Value));
        }

        uiParent.SetActive(true);
        inventoryToggleText.SetText("Close Inventory");
    }

    private void SetOverlappingUIObjectsState(bool state)
    {
        foreach (var uiObject in overlappingUIObjects)
        {
            uiObject.SetActive(state);
        }
    }

    private void OnItemAddOrRemove(InventoryItem item)
    {
        if(!_isOpen)
            return;
        _spawnedItemUIObjects.ReturnAllObjects();
        ShowInventoryUI();
    }

    public void ShowItemDropErrorMessage()
    {
        StartCoroutine(Show());
        return;

        IEnumerator Show()
        {
            errorMessage.SetActive(true);
            yield return new WaitForSeconds(1f);
            errorMessage.SetActive(false);
        }
    }
}
#endif