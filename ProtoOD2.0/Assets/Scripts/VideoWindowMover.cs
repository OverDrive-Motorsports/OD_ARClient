using UnityEngine;
using Meta.XR;

public class VideoWindowMover : MonoBehaviour
{
    [Header("Références")]
    public Transform rayStartPoint;      // RightHandAnchor
    public GameObject videoWindow;
    public EnvironmentRaycastManager envRayManager;   // 👈 à assigner dans l’Inspector

    [Header("Boutons")]
    public OVRInput.Button placeButton = OVRInput.Button.One;              // A
    public OVRInput.Button grabButton  = OVRInput.Button.PrimaryHandTrigger; // Grip droit

    [Header("Distance")]
    public float minDistance = 0.3f;
    public float maxDistance = 3f;
    public float distanceSpeed = 1.5f;

    [Header("Rotation")]
    public float rotationSpeed = 90f;    // degrés par seconde

    [Header("Smooth")]
    public float moveSpeed = 12f;

    [Header("Collisions")]
    public LayerMask collisionLayers;    // Default + environnement (pour Physics.Raycast, pas MR)

    [Header("Laser")]
    public LineRenderer laserLine;

    float _currentDistance = 1.5f;
    float _yawOffset = 0f;
    Vector3 _targetPosition;
    Quaternion _targetRotation;

    void Start()
    {
        _targetPosition = videoWindow.transform.position;
        _targetRotation = videoWindow.transform.rotation;

        if (laserLine != null)
            laserLine.positionCount = 2;

        _currentDistance = Vector3.Distance(
            rayStartPoint.position,
            videoWindow.transform.position
        );
    }

    void Update()
    {
        Ray ray = new Ray(rayStartPoint.position, rayStartPoint.forward);

        // Laser toujours visible
        UpdateLaser(ray);

        bool grab = OVRInput.Get(grabButton);

        // 1) PLACEMENT RAPIDE AVEC A (téléportation sur la surface MR)
        if (OVRInput.GetDown(placeButton))
        {
            if (envRayManager != null &&
                envRayManager.Raycast(ray, out var envHit, maxDistance))
            {
                // Hit MR (scan de la pièce)
                _currentDistance = Vector3.Distance(ray.origin, envHit.point);
                _yawOffset = 0f;

                _targetPosition = envHit.point;
                _targetRotation = Quaternion.LookRotation(-envHit.normal);
            }
            else
            {
                // Pas de hit MR → fallback dans l'air
                _currentDistance = maxDistance;
                _targetPosition = ray.origin + ray.direction * _currentDistance;
                _targetRotation = Quaternion.LookRotation(-ray.direction);
            }
        }

        // 2) MODE AJUSTEMENT QUAND GRAB MAINTENU
        if (grab)
        {
            Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            // Distance avec axe Y
            _currentDistance += stick.y * distanceSpeed * Time.deltaTime;
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);

            // Rotation autour de l'axe vertical de la fenêtre avec axe X
            _yawOffset += stick.x * rotationSpeed * Time.deltaTime;

            Vector3 dir = ray.direction;

            // Position souhaitée en ligne droite devant la manette
            Vector3 desiredPos = ray.origin + dir * _currentDistance;

            Quaternion baseRotation;

            // Collision physique classique (si tu utilises encore des meshes)
            if (Physics.Raycast(ray.origin, dir, out RaycastHit hit, _currentDistance, collisionLayers))
            {
                desiredPos = hit.point;
                baseRotation = Quaternion.LookRotation(-hit.normal);
            }
            else
            {
                baseRotation = Quaternion.LookRotation(-dir);
            }

            // Rotation locale autour de l'axe UP de la fenêtre
            Quaternion yawRot = Quaternion.AngleAxis(_yawOffset, Vector3.up);
            _targetRotation = baseRotation * yawRot;

            _targetPosition = desiredPos;
        }

        // 3) MOUVEMENT FLUIDE
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
    }

    void UpdateLaser(Ray ray)
    {
        if (laserLine == null) return;

        laserLine.SetPosition(0, ray.origin);
        laserLine.SetPosition(1, ray.origin + ray.direction * _currentDistance);
    }
}
