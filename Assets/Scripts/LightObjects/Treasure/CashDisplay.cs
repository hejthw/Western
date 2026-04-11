using UnityEngine;
using TMPro;

public class CashDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    private void Update()
    {
        if (CashManager.Instance == null) return;

        text.text = "Cash: " + CashManager.Instance.GetCash();
    }
}