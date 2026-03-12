using UnityEngine;

public class ToggleVisibility : MonoBehaviour
{
    public GameObject targetObject;

    public void ToggleState()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }
}
