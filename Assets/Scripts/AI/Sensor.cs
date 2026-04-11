using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Sensor : MonoBehaviour 
{
    public float detectionRadius = 10f;
    public List<string> targetTags = new();
    public float updateInterval = 0.5f;
        
    public readonly List<Transform> detectedObjects = new(4);
    [SerializeField] private SphereCollider detectionSphere;
    [SerializeField] private SphereCollider lineOfSightSphere;
    float _timer;

    void Start() 
    {
        detectionSphere = GetComponent<SphereCollider>();
        detectionSphere.isTrigger = true;
        detectionSphere.radius = detectionRadius;
        
        ScanOverlap();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateInterval)
        {
            _timer = 0f;
            ScanOverlap();
        }
    }
    
    void ScanOverlap()
    {
        detectedObjects.Clear();
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var c in colliders)
        {
            ProcessTrigger(c, t => detectedObjects.Add(t));
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        ProcessTrigger(other, t => detectedObjects.Add(t));
    }

    void OnTriggerExit(Collider other) 
    {
        detectedObjects.Remove(other.transform);
    }

    void ProcessTrigger(Collider other, Action<Transform> action) 
    {
        if (other.CompareTag("Untagged")) return;

        foreach (string t in targetTags)
        {
            if (other.CompareTag(t))
            {
                action(other.transform);
                return;
            }
        }
    }

    public Transform GetClosestTarget(string tag) 
    {
        if (detectedObjects.Count == 0) return null;
            
        Transform closestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (Transform potentialTarget in detectedObjects) 
        {
            if (potentialTarget == null) continue;
            
            if (potentialTarget.CompareTag(tag)) 
            {
                float dSqr = (potentialTarget.position - currentPosition).sqrMagnitude;
                if (dSqr < closestDistanceSqr) 
                {
                    closestDistanceSqr = dSqr;
                    closestTarget = potentialTarget;
                }
            } 
        } 
        return closestTarget;
    }
}