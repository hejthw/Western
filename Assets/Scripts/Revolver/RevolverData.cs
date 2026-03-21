using UnityEngine;

[CreateAssetMenu(fileName = "RevolverData", menuName = "ScriptableObjects/RevolverData")]
public class RevolverData : ScriptableObject
{
    public int damage = 100;
    public float timeBeforeShot = 1;
    public int bullets = 6;
    public float timeBeforeDespawn = 30f;
}