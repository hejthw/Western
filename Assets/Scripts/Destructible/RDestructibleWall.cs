using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InitialPartData
{
    public GameObject partObject;
    public Vector3 initialPosition;
    public Quaternion initialRotation;
}

public class RDestructibleWall : NetworkBehaviour
{
    [Header("Settings")]
    public ObstaclesSettingsData obstaclesSettings;
    [SerializeField] private float disappearDelay = 3f;

    [SerializeField] private List<InitialPartData> initialWallPartsData = new List<InitialPartData>();
    [SerializeField] private List<GameObject> wallParts = new List<GameObject>();
    [SerializeField] private Collider wallCollider;
    [SerializeField] private AudioClip destroySound;

    private AudioSource audioSource;
    private bool isDestroyed = false;
    public bool IsDestroyed => isDestroyed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        BakeScaleIfNeeded();
        SaveInitialData();
        ResetWall();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SaveInitialData();
        }
    }
#endif

    private void BakeScaleIfNeeded()
    {
        if (wallParts.Count == 0)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf && child.GetComponent<Collider>() != wallCollider)
                {
                    wallParts.Add(child.gameObject);
                }
            }
        }

        Vector3 parentScale = transform.localScale;
        if (parentScale == Vector3.one) return;

        Dictionary<GameObject, Vector3> originalLocalPositions = new Dictionary<GameObject, Vector3>();
        foreach (GameObject part in wallParts)
        {
            if (part != null)
            {
                originalLocalPositions[part] = part.transform.localPosition;
            }
        }

        foreach (GameObject part in wallParts)
        {
            if (part != null)
            {
                part.transform.localScale = Vector3.Scale(part.transform.localScale, parentScale);
            }
        }

        foreach (GameObject part in wallParts)
        {
            if (part != null && originalLocalPositions.TryGetValue(part, out Vector3 origLocalPos))
            {
                part.transform.localPosition = Vector3.Scale(origLocalPos, parentScale);
            }
        }

        if (wallCollider is BoxCollider boxCollider)
        {
            boxCollider.center = Vector3.Scale(boxCollider.center, parentScale);
            boxCollider.size = Vector3.Scale(boxCollider.size, parentScale);
        }

        transform.localScale = Vector3.one;
    }

    private void SaveInitialData()
    {
        initialWallPartsData.Clear();

        foreach (GameObject part in wallParts)
        {
            if (part != null)
            {
                initialWallPartsData.Add(new InitialPartData
                {
                    partObject = part,
                    initialPosition = part.transform.localPosition,
                    initialRotation = part.transform.localRotation
                });
            }
        }
    }

    private void ResetWall()
    {
        isDestroyed = false;

        if (wallCollider != null)
        {
            wallCollider.enabled = true;
        }

        StopAllCoroutines();

        foreach (InitialPartData data in initialWallPartsData)
        {
            if (data.partObject == null) continue;

            Rigidbody rb = data.partObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                Destroy(rb);
            }

            data.partObject.transform.localPosition = data.initialPosition;
            data.partObject.transform.localRotation = data.initialRotation;

            data.partObject.layer = LayerMask.NameToLayer("Default");
            data.partObject.SetActive(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyWallServer(Vector3 impactPoint)
    {
        if (isDestroyed) return;
        DestroyWallLocal(impactPoint);
        DestroyWallClient(impactPoint);
    }

    [ObserversRpc]
    private void DestroyWallClient(Vector3 impactPoint)
    {
        if (IsServer) return;
        DestroyWallLocal(impactPoint);
    }

    private void DestroyWallLocal(Vector3 impactPoint)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (wallCollider != null)
            wallCollider.enabled = false;

        if (audioSource != null && destroySound != null)
        {
            audioSource.volume = PlayerPrefs.GetFloat("SoundVolume");
            audioSource.PlayOneShot(destroySound);
        }

        StartCoroutine(DestroyPartsAfterDelay(impactPoint));
    }

    private IEnumerator DestroyPartsAfterDelay(Vector3 impactPoint)
    {
        int cubeLayer = LayerMask.NameToLayer("IgnorePlayer");

        foreach (GameObject part in wallParts)
        {
            if (part != null)
            {
                part.layer = cubeLayer;

                Rigidbody rb = part.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = part.AddComponent<Rigidbody>();

                rb.isKinematic = false;
                rb.AddExplosionForce(obstaclesSettings.explosionForce * 1.2f, impactPoint, obstaclesSettings.explosionRadius, 2f, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(disappearDelay);

        foreach (GameObject part in wallParts)
        {
            if (part != null)
                part.SetActive(false);
        }
    }
}