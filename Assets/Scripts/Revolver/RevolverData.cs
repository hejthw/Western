using UnityEngine;

[CreateAssetMenu(fileName = "RevolverData", menuName = "ScriptableObjects/RevolverData")]
public class RevolverData : ScriptableObject
{
    public int damage = 100;
    public float timeBeforeShot = 1;
    public int bullets = 6;
    public float timeBeforeDespawn = 30f;
    
    [Header("Recoil")]
    public float recoilUp = 4f;           // базовый подъём (градусы) за выстрел
    public float recoilSideMax = 1.5f;    // макс. боковая отдача первого выстрела
    public float sprayMultiplier = 2.8f;  // множитель при быстрой стрельбе (Deagle-feel)
    public float sprayRandomness = 3.5f;  // разброс при накоплении
    public float resetDelay = 0.55f;      // секунд без выстрела → сброс паттерна
    public float recoverySpeed = 6f;      // скорость возврата камеры (Lerp)
    public float recoilApplySpeed = 18f; 
}