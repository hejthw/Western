using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class CashHUD : MonoBehaviour
{
    [Header("Cash UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text cashProgressText;
    [SerializeField] private TMP_Text cashDropHintText;
    [SerializeField] private TMP_Text finishHeistHintText;
    
    private PickupController _pickupController;
    private PlayerController _playerController;
    private bool _lastInCashZone;
    private bool _lastCashUiVisible;
    private bool _lastCashManagerAvailable;
    private bool _loggedWaitingForOwner;
    private bool _loggedPickupControllerMissing;

    public void Awake()
    {
        Debug.Log($"[HUD] Awake. player={name}");
        _pickupController = GetComponentInParent<PickupController>();
        _playerController = GetComponentInParent<PlayerController>();
    }
    
        private void Update()
    {
        bool isOwner = _playerController != null && _playerController.IsOwner;
        if (!isOwner)
        {
            if (!_loggedWaitingForOwner)
            {
                Debug.Log($"[HUD] Skip Update because !IsOwner. player={name}");
                _loggedWaitingForOwner = true;
            }
            SetCashUiVisible(false);
            return;
        }

        if (_pickupController == null)
        {
            _pickupController = GetComponentInParent<PickupController>();
            if (_pickupController == null)
            {
                if (!_loggedPickupControllerMissing)
                {
                    Debug.Log($"[HUD] PickupController missing in Update. player={name}");
                    _loggedPickupControllerMissing = true;
                }
                return;
            }
        }

        if (cashProgressText == null || cashDropHintText == null)
            EnsureCashUiReferences();

        bool inCashZone = _pickupController.IsInsideCashZone();
        if (inCashZone != _lastInCashZone)
        {
            Debug.Log($"[HUD] CashZone state changed. inCashZone={inCashZone}, isOwner={isOwner}, player={name}");
            _lastInCashZone = inCashZone;
        }

        if (!inCashZone)
        {
            if (_lastCashUiVisible)
            {
                Debug.Log($"[HUD] Hide cash UI (left cash zone). player={name}");
                _lastCashUiVisible = false;
            }
            SetCashUiVisible(false);
            return;
        }

        if (cashProgressText == null || cashDropHintText == null || finishHeistHintText == null)
            return;

        bool hasCashManager = CashManager.Instance != null;
        if (hasCashManager != _lastCashManagerAvailable)
        {
            Debug.Log($"[HUD] CashManager availability changed. available={hasCashManager}, player={name}");
            _lastCashManagerAvailable = hasCashManager;
        }

        if (!_lastCashUiVisible)
        {
            Debug.Log($"[HUD] Show cash UI (entered cash zone). player={name}");
            _lastCashUiVisible = true;
        }

        SetCashUiVisible(true);
        int currentCash = CashManager.Instance != null ? CashManager.Instance.GetCash() : 0;
        int requiredCash = HeistManager.Instance != null ? HeistManager.Instance.GetRequired() : 0;
        if (requiredCash <= 0)
            requiredCash = currentCash;

        cashProgressText.text = $"{currentCash}/{requiredCash}";

        if (_pickupController.TryGetHeldLootValue(out int lootValue))
        {
            string interactKey = _pickupController.GetInteractBindingLabel();
            cashDropHintText.text = $"{interactKey} - сбросить(+{lootValue}$)";
            cashDropHintText.gameObject.SetActive(true);
        }
        else
        {
            cashDropHintText.gameObject.SetActive(false);
        }

        bool canFinish = EscapeZone.Instance != null && EscapeZone.Instance.IsFinishAvailable();
        if (canFinish)
        {
            string finishKey = "Z";
            var input = GetComponentInParent<PlayerInput>();
            if (input != null)
                finishKey = input.GetFinishBindingDisplay();

            finishHeistHintText.text = $"{finishKey} (зажать) - закончить уровень";
            finishHeistHintText.gameObject.SetActive(true);
        }
        else
        {
            finishHeistHintText.gameObject.SetActive(false);
        }
    }

    private void EnsureCashUiReferences()
    {
        Canvas rootCanvas = nameText != null ? nameText.GetComponentInParent<Canvas>() : null;
        if (rootCanvas == null)
            return;

        if (cashProgressText == null)
            cashProgressText = CreateRuntimeText(rootCanvas.transform, "CashProgressText", new Vector2(1f, 1f), new Vector2(-30f, -30f), 36, TextAlignmentOptions.Right);

        if (cashDropHintText == null)
            cashDropHintText = CreateRuntimeText(rootCanvas.transform, "CashDropHintText", new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), 30, TextAlignmentOptions.Center);

        if (finishHeistHintText == null)
            finishHeistHintText = CreateRuntimeText(rootCanvas.transform, "FinishHeistHintText", new Vector2(0.5f, 0.5f), new Vector2(0f, -165f), 28, TextAlignmentOptions.Center);
    }

    private TMP_Text CreateRuntimeText(Transform parent, string objectName, Vector2 anchor, Vector2 anchoredPosition, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(420f, 60f);

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.text = string.Empty;

        return tmp;
    }

    private void SetCashUiVisible(bool visible)
    {
        if (cashProgressText != null)
            cashProgressText.gameObject.SetActive(visible);
        if (cashDropHintText != null)
            cashDropHintText.gameObject.SetActive(visible);
        if (finishHeistHintText != null)
            finishHeistHintText.gameObject.SetActive(visible);
    }
}