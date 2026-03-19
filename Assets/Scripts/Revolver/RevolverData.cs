using UnityEngine;

[CreateAssetMenu(fileName = "RevolverData", menuName = "ScriptableObjects/RevolverData")]
public class RevolverData : ScriptableObject
{
    public int damage = 100;
    public float timeBeforeShot = 1;
}