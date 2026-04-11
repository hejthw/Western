using UnityEngine;

public class Treasure : LightObject
{
    [SerializeField] private int value;

    public int GetValue() => value;

    public void SetData(TreasureData data)
    {
        if (data == null)
        {
            Debug.LogError("[Treasure] Data is NULL");
            return;
        }

        value = data.value;

        // примен€ем настройки из базы
        SetFragile(data.fragile);
        SetSemiFragile(data.semiFragile);
    }

    // --- сеттеры дл€ базового класса ---

    private void SetFragile(bool val)
    {
        // т.к. в LightObject поле private Ч используем обход
        var type = typeof(LightObject);
        var field = type.GetField("fragile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, val);
    }

    private void SetSemiFragile(bool val)
    {
        var type = typeof(LightObject);
        var field = type.GetField("semiFragile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, val);
    }
}