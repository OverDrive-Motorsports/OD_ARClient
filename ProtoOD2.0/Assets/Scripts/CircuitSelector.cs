using UnityEngine;

public class CircuitSelector : MonoBehaviour
{
    public GameObject circuitPrefab;
    public Transform playerHead;              // CenterEyeAnchor
    public Transform spawnParent;

    public float distanceFromHead = 1.5f;
    public float heightOffset = 0.0f;         // 0 = à hauteur des yeux, mets -0.5f pour plus bas

    GameObject _currentCircuit;

    public void OnLoadCircuitButton()
    {
        if (_currentCircuit != null)
            Destroy(_currentCircuit);

        if (circuitPrefab == null || playerHead == null)
        {
            Debug.LogWarning("CircuitSelector : prefab ou playerHead manquant.");
            return;
        }

        // Devant la tête à 1.5 m
        Vector3 forward = new Vector3(playerHead.forward.x, 0f, playerHead.forward.z).normalized;
        Vector3 pos = playerHead.position + forward * distanceFromHead;
        pos.y += heightOffset;

        Quaternion rot = Quaternion.LookRotation(-forward);

        _currentCircuit = Instantiate(
            circuitPrefab,
            pos,
            rot,
            spawnParent
        );
    }
}
