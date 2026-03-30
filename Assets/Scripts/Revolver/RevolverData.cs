using UnityEngine;

[CreateAssetMenu(fileName = "RevolverData", menuName = "ScriptableObjects/RevolverData")]
public class RevolverData : ScriptableObject
{
    public int damage = 100;
    public float timeBeforeShot = 1;
    public int bullets = 6;
    public float timeBeforeDespawn = 30f;
    
    [Header("Recoil")]
    public float recoilUp = 4f;
    public float recoilSideMax = 1.5f;
    public float sprayMultiplier = 2.8f;
    public float sprayRandomness = 3.5f;
    public float resetDelay = 0.55f;
    public float recoverySpeed = 6f;
    public float recoilApplySpeed = 18f;
}