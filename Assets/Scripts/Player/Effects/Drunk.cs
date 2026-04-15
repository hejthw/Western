using System.Collections;
using UnityEngine;

public class Drunk : MonoBehaviour
{
    private const int MaxStack = 3;

    private int _stack;
    private Coroutine _timerCoroutine;

    [SerializeField] private BuffData _data;

    private void OnEnable() => PlayerEffectsEvents.OnWhiskeyUse += RaiseStack;
    private void OnDisable() => PlayerEffectsEvents.OnWhiskeyUse -= RaiseStack;

    private void RaiseStack()
    {
        if (_stack < MaxStack)
        {
            _stack++;
            ApplyBuffs();
            Debug.Log($"[Drunk] Stack {_stack}/{MaxStack}");
        }
        else
        {
            Debug.Log("[Drunk] Max stack");
        }
        
        ResetTimer();
    }

    private void ApplyBuffs()
    {
        float speed = _data.walkSpeedBuff * _stack;
        float recoil = _data.recoilBuff * _stack;
        float walk = _data.walkDebuff * _stack;
        
        PlayerEffectsEvents.RaiseSpeedBuff(speed);
        PlayerEffectsEvents.RaiseRecoilBuff(recoil);
        PlayerEffectsEvents.RaiseWalkDebuff(walk);
    }

    private void ResetTimer()
    {
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

        _timerCoroutine = StartCoroutine(BuffTimer());
    }

    private IEnumerator BuffTimer()
    {
        yield return new WaitForSeconds(10f);
        RemoveBuffs();
    }

    private void RemoveBuffs()
    {
        if (_stack == 3)
        {
            PlayerEffectsEvents.RaiseThrowup();
            PlayerHealthEvents.RaiseKnockoutEvent(false);
        }
        _stack = 0;
        _timerCoroutine = null;

        PlayerEffectsEvents.RaiseSpeedBuff(0f);
        PlayerEffectsEvents.RaiseRecoilBuff(0f);
        PlayerEffectsEvents.RaiseWalkDebuff(0f);
        PlayerEffectsEvents.RaiseDrunkExpired();

        Debug.Log("[Drunk] Buffs expired");
    }
}