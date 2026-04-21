using System;
using UnityEngine;
using UnityEngine.UI;

public class LassoHUD : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] private Sprite idleIcon;
    [SerializeField] private Sprite flyingIcon;
    [SerializeField] private Sprite capturedIcon;
    
    [SerializeField] private Image image;

    private void OnEnable()
    {
        UIEvents.OnLassoStateChanged += SetState;
    }

    private void OnDisable()
    {
        UIEvents.OnLassoStateChanged -= SetState;
    }

    private void SetState(LassoHUDState state)
    {
        image.sprite = state switch
        {
            LassoHUDState.Idle => idleIcon,
            LassoHUDState.Flying => flyingIcon,
            LassoHUDState.Captured => capturedIcon,
            _  => idleIcon
        };
    }
}