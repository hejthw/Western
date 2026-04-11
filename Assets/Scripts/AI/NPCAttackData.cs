using UnityEngine;

[CreateAssetMenu(fileName = "NPCAttackData", menuName = "ScriptableObjects/NPCAttackData")]
public class NPCAttackData : ScriptableObject
{
    public float FireRate;        // выстрелов в секунду, например 1.5
    public float MaxSpread;       // макс разброс в градусах, например 10
    public float SpreadDistance;  // дистанция при которой разброс максимален, например 20
    public int Damage;
    public LayerMask HitMask;
}