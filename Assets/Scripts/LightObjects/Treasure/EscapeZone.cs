using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public class EscapeZone : NetworkBehaviour
{
    private List<GameObject> playersInZone = new List<GameObject>();
    private float holdTimer = 0f;
    private float requiredHoldTime = 3f;

    private void Update()
    {
        if (!IsServer) return;

        if (playersInZone.Count == 0)
        {
            holdTimer = 0;
            return;
        }

        bool allHolding = true;

        foreach (var player in playersInZone)
        {
            var input = player.GetComponent<PlayerInput>();
            if (input == null || !input.IsHoldingFinish)
            {
                allHolding = false;
                break;
            }
        }

        if (allHolding)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= requiredHoldTime)
            {
                HeistManager.Instance.EndHeist();
            }
        }
        else
        {
            holdTimer = 0;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersInZone.Remove(other.gameObject);
            holdTimer = 0;
        }
    }
}