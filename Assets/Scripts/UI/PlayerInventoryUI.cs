using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;
using System.Collections.Generic;

public class PlayerInventoryUI : NetworkBehaviour
{
    [SerializeField] private GameObject slotPrefab; 
    [SerializeField] private Transform slotsParent; 
    [Header("Icons")]
    [SerializeField] private InventoryIconDatabase iconDatabase;
    [SerializeField] private Sprite emptySlotIcon;
    [SerializeField] private Sprite fallbackTypeRevolverIcon;
    [SerializeField] private Sprite fallbackTypeLootIcon;
    [SerializeField] private Sprite[] slotNumberIcons;
    [SerializeField] private Vector2 slotNumberIconSize = new Vector2(126f, 72f);

    [Header("Selected Slot Scale")]
    [SerializeField] private float selectedSlotScale = 1.12f;
    [SerializeField] private float normalSlotScale = 1f;

    private List<GameObject> slotObjects = new List<GameObject>();
    private List<Image> slotItemIcons = new List<Image>();
    private PlayerInventory playerInventory;
    private PlayerInput playerInput;
    private int _slotsCount;
    private int _selectedSlot = -1;

    public override void OnStartClient()
    {
        base.OnStartClient();
        PlayerController playerController = GetComponentInParent<PlayerController>();
        if (playerController != null && !playerController.IsOwner) return;

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryUI: PlayerInventory not found!");
            return;
        }

        _slotsCount = playerInventory.SlotsCount;
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.OnSlotKeyPressed += OnSlotKeyPressed;
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
            slotItemIcons.Add(EnsureItemIcon(slot));
            EnsureSlotNumberIcon(slot, i);
            DisableSlotBackground(slot);
            ApplySlotScale(i);
        }
    }

    private void UpdateSlot(int slot, bool isOccupied)
    {
        if (slot >= slotObjects.Count) return;
        GameObject slotObj = slotObjects[slot];
      
        TMP_Text text = slotObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = string.Empty;
        }

        if (slot < slotItemIcons.Count)
        {
            Image itemIcon = slotItemIcons[slot];
            if (itemIcon != null)
            {
                Sprite sprite = ResolveItemIcon(slot, isOccupied);
                itemIcon.sprite = sprite;
                itemIcon.enabled = sprite != null;
            }
        }

        ApplySlotScale(slot);
    }

    private void OnDestroy()
    {
        if (playerInventory != null && IsOwner)
            playerInventory.OnSlotChanged -= UpdateSlot;
        if (playerInput != null && IsOwner)
            playerInput.OnSlotKeyPressed -= OnSlotKeyPressed;
    }

    private Image EnsureItemIcon(GameObject slotObj)
    {
        Transform existing = slotObj.transform.Find("ItemIcon");
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform rt = iconObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12f, 12f);
        rt.offsetMax = new Vector2(-12f, -12f);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;
        return iconImage;
    }

    private Image EnsureSlotNumberIcon(GameObject slotObj, int slotIndex)
    {
        Transform existing = slotObj.transform.Find("SlotNumberIcon");
        Image iconImage;
        if (existing != null)
        {
            iconImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject iconObj = new GameObject("SlotNumberIcon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform rt = iconObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(126f, 72f);
            iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        RectTransform iconRt = iconImage.GetComponent<RectTransform>();
        if (iconRt != null)
            iconRt.sizeDelta = slotNumberIconSize;

        if (slotNumberIcons != null && slotIndex >= 0 && slotIndex < slotNumberIcons.Length)
            iconImage.sprite = slotNumberIcons[slotIndex];
        iconImage.enabled = iconImage.sprite != null;
        return iconImage;
    }

    private void DisableSlotBackground(GameObject slotObj)
    {
        Image bg = slotObj.GetComponent<Image>();
        if (bg != null)
            bg.enabled = false;
    }

    private Sprite ResolveItemIcon(int slot, bool isOccupied)
    {
        if (!isOccupied || playerInventory == null)
            return emptySlotIcon;

        int prefabId = playerInventory.GetItemPrefabId(slot);
        if (prefabId < 0)
            return emptySlotIcon;

        NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
        if (prefab == null)
            return fallbackTypeLootIcon != null ? fallbackTypeLootIcon : emptySlotIcon;

        if (iconDatabase != null && iconDatabase.TryGetIcon(prefab, out Sprite mappedIcon) && mappedIcon != null)
            return mappedIcon;

        if (prefab.GetComponent<RevolverPickup>() != null)
            return fallbackTypeRevolverIcon != null ? fallbackTypeRevolverIcon : emptySlotIcon;

        return fallbackTypeLootIcon != null ? fallbackTypeLootIcon : emptySlotIcon;
    }

    private void OnSlotKeyPressed(int slot)
    {
        if (!IsOwner)
            return;
        _selectedSlot = slot;
        for (int i = 0; i < slotObjects.Count; i++)
            ApplySlotScale(i);
    }

    private void ApplySlotScale(int slot)
    {
        if (slot < 0 || slot >= slotObjects.Count)
            return;

        bool occupied = playerInventory != null && !playerInventory.IsSlotEmpty(slot);
        bool shouldHighlight = occupied && slot == _selectedSlot;
        float targetScale = shouldHighlight ? selectedSlotScale : normalSlotScale;
        slotObjects[slot].transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }
}