using UnityEngine;

[CreateAssetMenu(fileName = "BuffData", menuName = "ScriptableObjects/BuffData")]
public class BuffData : ScriptableObject
{
    [Header("Drunk")] 
    public float walkSpeedBuff = 0.1f;
    public float recoilBuff = 0.1f;
    public float walkDebuff = 0.1f;
}