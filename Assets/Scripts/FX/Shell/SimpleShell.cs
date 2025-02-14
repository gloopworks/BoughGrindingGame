using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleShell : MonoBehaviour
{
    [Header("Asset References")]
    [SerializeField] private Mesh shellMesh;
    [SerializeField] private Material shellMaterial;

    [Space, SerializeField] private bool updateSettingsRuntime;

    private MeshRenderer[] shells;
    private int shellCount;

    private void GenerateShells()
    {
        shellCount = (int)shellMaterial.GetFloat("_ShellCount");
        shells = new MeshRenderer[shellCount];

        for (int i = 0; i < shellCount; i++)
        {
            GameObject shell = new($"{gameObject.name} Shell {i}");
            MeshFilter filter = shell.AddComponent<MeshFilter>();
            shells[i] = shell.AddComponent<MeshRenderer>();

            filter.mesh = shellMesh;
            shells[i].material = shellMaterial;

            shell.transform.SetParent(transform, false);

            shells[i].material.CopyPropertiesFromMaterial(shellMaterial);

            shells[i].material.SetFloat("_ShellIndex", i);
            shells[i].material.SetFloat("_ShellCount", shellCount);
        }
    }

    private void UpdateShells()
    {
        for (int i = 0; i < shellCount; i++)
        {
            shells[i].material.CopyPropertiesFromMaterial(shellMaterial);

            shells[i].material.SetFloat("_ShellIndex", i);
            shells[i].material.SetFloat("_ShellCount", shellCount);
        }
    }

    private void DestroyShells()
    {
        for (int i = 0; i < shells.Length; i++)
        {
            Destroy(shells[i]);
        }

        shells = null;
    }
    private void OnEnable()
    {
        GenerateShells();
    }

    private void Update()
    {
        if (updateSettingsRuntime)
        {
            UpdateShells();
        }
    }

    private void OnDisable()
    {
        if (shells != null)
        {
            DestroyShells();
        }
    }
}
