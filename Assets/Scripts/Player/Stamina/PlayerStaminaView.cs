using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerStaminaView : MonoBehaviour
{
    public Image staminaText;
    public TMP_Text text;
    
    void Awake()
    {
        staminaText.fillAmount = 1;
    }
    
    private void OnEnable()
    {
        PlayerEvents.OnLocalStaminaChange += HandleStaminaChange;
    }

    private void OnDisable()
    {
        PlayerEvents.OnLocalStaminaChange -= HandleStaminaChange;
    }

    private void HandleStaminaChange(float stamina)
    {
        text.text = $"Stamina: {stamina.ToString()}";
        stamina /= 100;
        staminaText.fillAmount = stamina; 
    }
    
}