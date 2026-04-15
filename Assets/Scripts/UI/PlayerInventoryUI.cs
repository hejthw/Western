using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;
using FishNet.Connection;
using System.Collections.Generic;

public class PlayerInventoryUI : NetworkBehaviour
{
    [SerializeField] private GameObject slotPrefab; 
    [SerializeField] private Transform slotsParent; 

    private List<GameObject> slotObjects = new List<GameObject>();
    private PlayerInventory playerInventory;
    private int _slotsCount;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) return;

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryUI: PlayerInventory not found!");
            return;
        }

        _slotsCount = playerInventory.SlotsCount;
        CreateSlots();
        playerInventory.OnSlotChanged += UpdateSlot;
      
        for (int i = 0; i < _slotsCount; i++)
        {
            bool isOccupied = !playerInventory.IsSlotEmpty(i);
            UpdateSlot(i, isOccupied);
        }
    }

    private void CreateSlots()
    {
        for (int i = 0; i < _slotsCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotsParent);
            slotObjects.Add(slot);
            
        }
    }

    private void UpdateSlot(int slot, bool isOccupied)
    {
        if (slot >= slotObjects.Count) return;
        GameObject slotObj = slotObjects[slot];
      
        TMP_Text text = slotObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = isOccupied ? "Item" : "Empty";
        }
        Image image = slotObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = isOccupied ? Color.green : Color.gray;
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null && IsOwner)
            playerInventory.OnSlotChanged -= UpdateSlot;
    }
}