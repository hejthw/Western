using TMPro;
using UnityEngine;

public class HeistUI : MonoBehaviour
{
    public TextMeshProUGUI text;

    private bool finished = false;

    private void Update()
    {
        if (finished) return;

        if (HeistManager.Instance == null) return;

        int current = HeistManager.Instance.GetCollected();
        int max = HeistManager.Instance.GetRequired();

        text.text = $"Cash: {current} / {max}";
    }

    public void ShowResult(bool win)
    {
        finished = true;
        text.text = win ? "онаедю" : "онпюфемхе";
    }
}