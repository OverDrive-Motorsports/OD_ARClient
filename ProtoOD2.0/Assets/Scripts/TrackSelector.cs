using UnityEngine;
using TMPro; // Si TMP_Dropdown

public class DropdownToggle : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown; // Assigne ton dropdown
    [SerializeField] private GameObject SilverStone;    // Assigne ton objet 3D
    [SerializeField] private GameObject Monza;    // Assigne ton objet 3D

    void Start()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnOptionChange);
    }

    public void OnOptionChange(int index)
    {
        SilverStone.SetActive(index == 0); // Ex: visible si première option (index 0)
        Monza.SetActive(index == 1); // Ex: visible si deuxième option (index 1)
        // Ou: if (index == 0) objet3D.SetActive(true); else objet3D.SetActive(false);
    }
}
