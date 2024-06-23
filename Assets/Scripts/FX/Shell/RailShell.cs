using UnityEngine;

namespace MixJam12.FX.Rails
{
    public class RailShell : MonoBehaviour
    {
        [Header("Asset References")]
        [SerializeField] private Material shellMaterial;

        [Space, SerializeField] private bool updateSettingsRuntime;

        private MeshRenderer[] shells;
        private int shellCount;

        public void GenerateShells(Mesh shellMesh)
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

        public void UpdateLength(float length)
        {
            for (int i = 0; i < shellCount; i++)
            {
                shells[i].material.SetFloat("_Length", length);
            }
        }

        private void UpdateShellsAll()
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

        private void Update()
        {
            if (updateSettingsRuntime)
            {
                UpdateShellsAll();
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
}