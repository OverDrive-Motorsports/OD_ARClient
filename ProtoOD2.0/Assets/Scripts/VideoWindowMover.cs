using UnityEngine;
using Meta.XR;

public class VideoWindowMover : MonoBehaviour
{
    [Header("Références")]
    public Transform rayStartPoint;              // RightHandAnchor
    public EnvironmentRaycastManager envRayManager;
    public GameObject videoWindow;               // Ton Canvas VideoWindow

    [Header("Paramètres")]
    public float rayLength = 5f;
    public OVRInput.Button moveButton = OVRInput.Button.One; // Bouton A

    [Header("Smooth Move (optionnel)")]
    public float moveSpeed = 8f;

    private bool _isMoving = false;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    void Update()
    {
        Ray ray = new Ray(rayStartPoint.position, rayStartPoint.forward);

        // Appui sur A : on enregistre la nouvelle destination
        if (OVRInput.GetDown(moveButton))
        {
            Debug.Log("✅ Bouton A détecté !");

            // Log 2 : est-ce que le raycast touche quelque chose ?
            if (envRayManager.Raycast(ray, out var hit, rayLength))
            {
                Debug.Log("✅ Raycast hit : " + hit.point);
                _targetPosition = hit.point;
                _targetRotation = Quaternion.LookRotation(-hit.normal);
                _isMoving = true;
            }
            else
            {
                Debug.Log("❌ Raycast : aucun hit !");
            }
        }

        // Déplacement fluide vers la destination
        if (_isMoving)
        {
            videoWindow.transform.position = Vector3.Lerp(
                videoWindow.transform.position,
                _targetPosition,
                Time.deltaTime * moveSpeed
            );

            videoWindow.transform.rotation = Quaternion.Slerp(
                videoWindow.transform.rotation,
                _targetRotation,
                Time.deltaTime * moveSpeed
            );

            // On arrête le Lerp quand on est assez proche
            if (Vector3.Distance(videoWindow.transform.position, _targetPosition) < 0.005f)
            {
                videoWindow.transform.position = _targetPosition;
                videoWindow.transform.rotation = _targetRotation;
                _isMoving = false;
            }
        }
    }
}