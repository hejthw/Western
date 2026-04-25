using System.Collections;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class CashHUD : MonoBehaviour
{
    [Header("Cash UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text cashProgressText;
    [SerializeField] private TMP_Text cashDropHintText;
    [SerializeField] private TMP_Text finishHeistHintText;
    [SerializeField] private TMP_Text heistTimerText;
    [SerializeField] private GameObject timerFinishedElement;
    [SerializeField] private float heistTimerSeconds = 40f;
    [SerializeField] private float timerFinishedElementSeconds = 3f;
    
    private PickupController _pickupController;
    private PlayerController _playerController;
    private bool _cashProgressUnlocked;
    private Coroutine _timerRoutine;
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

        if (heistTimerText != null)
            heistTimerText.gameObject.SetActive(false);
        if (timerFinishedElement != null)
            timerFinishedElement.SetActive(false);
    }

    private void OnEnable()
    {
        HeistDoor.OpenedByLocalPlayer += OnHeistDoorOpenedByLocalPlayer;
    }

    private void OnDisable()
    {
        HeistDoor.OpenedByLocalPlayer -= OnHeistDoorOpenedByLocalPlayer;
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
            SetCashProgressVisible(false);
            SetZoneUiVisible(false);
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

        bool showCashProgress = _cashProgressUnlocked;
        SetCashProgressVisible(showCashProgress);
        if (showCashProgress)
            UpdateCashProgressText();

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
            SetZoneUiVisible(false);
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

        SetZoneUiVisible(true);

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

    private void SetCashProgressVisible(bool visible)
    {
        if (cashProgressText != null)
            cashProgressText.gameObject.SetActive(visible);
    }

    private void SetZoneUiVisible(bool visible)
    {
        if (cashDropHintText != null)
            cashDropHintText.gameObject.SetActive(visible);
        if (finishHeistHintText != null)
            finishHeistHintText.gameObject.SetActive(visible);
    }

    private void UpdateCashProgressText()
    {
        if (cashProgressText == null)
            return;

        int currentCash = CashManager.Instance != null ? CashManager.Instance.GetCash() : 0;
        int requiredCash = HeistManager.Instance != null ? HeistManager.Instance.GetRequired() : 0;
        if (requiredCash <= 0)
            requiredCash = currentCash;

        cashProgressText.text = $"{currentCash}/{requiredCash}";
    }

    private void OnHeistDoorOpenedByLocalPlayer()
    {
        if (_playerController == null)
            _playerController = GetComponentInParent<PlayerController>();

        if (_playerController == null || !_playerController.IsOwner)
            return;

        _cashProgressUnlocked = true;

        if (_timerRoutine != null)
            StopCoroutine(_timerRoutine);
        _timerRoutine = StartCoroutine(HeistTimerRoutine());
    }

    private IEnumerator HeistTimerRoutine()
    {
        if (timerFinishedElement != null)
            timerFinishedElement.SetActive(false);

        if (heistTimerText != null)
            heistTimerText.gameObject.SetActive(true);

        float remaining = Mathf.Max(0f, heistTimerSeconds);
        while (remaining > 0f)
        {
            if (heistTimerText != null)
                heistTimerText.text = Mathf.CeilToInt(remaining).ToString();

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (heistTimerText != null)
            heistTimerText.gameObject.SetActive(false);

        if (timerFinishedElement != null)
        {
            timerFinishedElement.SetActive(true);
            yield return new WaitForSeconds(timerFinishedElementSeconds);
            timerFinishedElement.SetActive(false);
        }

        _timerRoutine = null;
    }
}