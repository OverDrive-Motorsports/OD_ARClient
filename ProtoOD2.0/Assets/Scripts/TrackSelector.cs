using UnityEngine;
using TMPro;
using UnityEngine.UI; // Pour Button

public class DropdownToggle : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private GameObject SilverStone;
    [SerializeField] private GameObject Monza;
    [SerializeField] private Button boutonToggle; // Assigne ton bouton ici

    private GameObject[] pistes = new GameObject[2]; // Array pour scaler facilement

    void Start()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnOptionChange);

        if (boutonToggle != null)
            boutonToggle.onClick.AddListener(ToggleSelectedPiste);

        // Initialise le tableau de pistes et désactive toutes les pistes au démarrage
        pistes[0] = Monza;      // Index 0 : Monza
        pistes[1] = SilverStone; // Index 1 : SilverStone (correspond à l'ordre des options du dropdown)
        HideAllPistes();
    }

    public void OnOptionChange(int index)
    {
        HideAllPistes();
        if (index < pistes.Length) pistes[index].SetActive(true);
    }

    public void ToggleSelectedPiste()
    {
        int index = dropdown.value;
        if (index < pistes.Length)
        {
            bool etatActuel = pistes[index].activeSelf;
            pistes[index].SetActive(!etatActuel); // Toggle inverse l'état
        }
    }

    void HideAllPistes()
    {
        foreach (GameObject piste in pistes)
            piste.SetActive(false);
    }
}
