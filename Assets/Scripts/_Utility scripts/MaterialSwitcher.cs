using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class MaterialSwitcher : MonoBehaviour
{
    [SerializeField] private List<Material> materials;
    [SerializeField] private int materialIndex;

    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        if (materials.Count == 0)
        {
            Debug.LogWarning("No materials assigned to MaterialSwitcher component on " + gameObject.name);
        }
    }

    private void Update()
    {
        if (rend == null || materials.Count == 0)
            return;

        materialIndex = Mathf.Clamp(materialIndex, 0, materials.Count - 1);
        rend.material = materials[materialIndex];
    }
}