using UnityEngine;
using Meta.XR;

public class SceneCollisionDebug : MonoBehaviour
{
    public Transform RayStartPoint;
    public float RayLength = 5;
    public EnvironmentRaycastManager envRayManager;
    public TMPro.TextMeshPro debugText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(RayStartPoint.position, RayStartPoint.forward);
        bool hasHit = envRayManager.Raycast(ray, out var hit, RayLength);

        if (hasHit)
        {
            Vector3 hitPoint = hit.point;
            Vector3 hitNormal = hit.normal;

            debugText.transform.position = hitPoint;
            debugText.transform.rotation = Quaternion.LookRotation(hitNormal);

            debugText.text =  "Env Hit !";
        }
    }
}
 