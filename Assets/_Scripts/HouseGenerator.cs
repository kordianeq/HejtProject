using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class HouseGenerator : MonoBehaviour
{
    [Header("Prefab & placement")]
    public GameObject housePrefab;                 // Twój prefab domu
    public Transform wallsRootOverride;            // opcjonalnie: wskazać transform który zawiera renderer'y ścian
    public int housesPerSide = 10;                 // ile na każdej stronie ulicy
    public float streetLength = 50f;               // długość ulicy (wzdłuż osi local Z)
    public float sideOffset = 6f;                  // odległość od środka ulicy do domu (x)
    public Vector2 spacingRange = new Vector2(3f, 8f); // losowy odstęp wzdłuż ulicy
    public Vector2 lateralJitter = new Vector2(-0.7f, 0.7f); // losowy przesun w osi X
    public Vector2 forwardJitter = new Vector2(-1f, 1f);    // losowy przesun w osi Z
    public Vector2 scaleRange = new Vector2(0.95f, 1.05f);  // drobna wariacja skali

    [Header("Collision / overlap")]
    public float placementRadius = 2f;             // promień sprawdzania kolizji (zapobiega nachodzeniu)
    public LayerMask placementCollisionMask = 0;   // opcjonalne warstwy do sprawdzenia kolizji

    [Header("Color")]
    public Color colorMin = Color.white;
    public Color colorMax = new Color(0.8f, 0.8f, 0.8f);
    public bool randomizeWallsOnly = true;

    [Header("Runtime")]
    public bool generateOnStart = false;

    private List<GameObject> spawned = new List<GameObject>();

    void Start()
    {
        if (Application.isPlaying && generateOnStart)
        {
            Generate();
        }
    }

    public void Clear()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(spawned[i]);
                else
#endif
                    Destroy(spawned[i]);
            }
        }
        spawned.Clear();
    }

    public void Generate()
    {
        if (housePrefab == null)
        {
            Debug.LogWarning("HouseGenerator: brak ustawionego housePrefab.");
            return;
        }

        Clear();

        float halfLen = streetLength * 0.5f;

        GenerateSide(-1, halfLen);
        GenerateSide(+1, halfLen);
    }

    private void GenerateSide(int sideSign, float halfLen)
    {
        float z = -halfLen;
        int attempts = 0;
        int maxAttempts = housesPerSide * 10;

        for (int i = 0; i < housesPerSide && attempts < maxAttempts;)
        {
            attempts++;
            float step = Random.Range(spacingRange.x, spacingRange.y);
            z += step;
            if (z > halfLen) break;

            Vector3 localPos = new Vector3(
                sideSign * (sideOffset + Random.Range(lateralJitter.x, lateralJitter.y)),
                0f,
                z + Random.Range(forwardJitter.x, forwardJitter.y)
            );

            Vector3 worldPos = transform.TransformPoint(localPos);

            if (Physics.CheckSphere(worldPos, placementRadius, placementCollisionMask))
            {
                continue;
            }

            GameObject inst = (GameObject)PrefabUtility_Instantiate(housePrefab, worldPos, Quaternion.identity);
            inst.transform.SetParent(this.transform, worldPositionStays: true);

            // Szukamy markera frontu w prefabie
            Transform marker = inst.transform.Find("FrontMarker");

            if (marker != null)
            {
                // Kierunek, w który marker patrzy w świecie
                Vector3 markerForward = marker.forward;

                // Kierunek, w który powinien patrzeć front (zależnie od strony ulicy)
                Vector3 targetDir = (sideSign < 0) ? transform.right : -transform.right;

                // Liczymy różnicę rotacji
                Quaternion delta = Quaternion.FromToRotation(markerForward, targetDir);

                // Obracamy cały domek tak, żeby jego marker patrzył tam gdzie trzeba
                inst.transform.rotation = delta * inst.transform.rotation;
            }
            else
            {
                Debug.LogWarning("Prefab nie ma FrontMarker – używam domyślnej orientacji.");
                Vector3 dir = (sideSign < 0) ? transform.right : -transform.right;
                inst.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            // drobna losowa skala
            float s = Random.Range(scaleRange.x, scaleRange.y);
            inst.transform.localScale = Vector3.one * s;

            ApplyRandomWallColor(inst);

            spawned.Add(inst);
            i++;
        }
    }

    private void ApplyRandomWallColor(GameObject instance)
    {
        Color c = new Color(
            Random.Range(colorMin.r, colorMax.r),
            Random.Range(colorMin.g, colorMax.g),
            Random.Range(colorMin.b, colorMax.b),
            1f
        );

        List<Renderer> wallRenderers = new List<Renderer>();
        if (wallsRootOverride != null)
        {
            wallRenderers.AddRange(wallsRootOverride.GetComponentsInChildren<Renderer>());
        }
        else
        {
            Transform wallsT = instance.transform.Find("Walls");
            if (wallsT != null)
            {
                wallRenderers.AddRange(wallsT.GetComponentsInChildren<Renderer>());
            }
            else
            {
                Renderer[] all = instance.GetComponentsInChildren<Renderer>();
                foreach (var r in all)
                {
                    string n = r.gameObject.name.ToLower();
                    if (n.Contains("wall") || n.Contains("walls") || n.Contains("ścian") || n.Contains("scian"))
                    {
                        wallRenderers.Add(r);
                    }
                }
            }
        }

        if (wallRenderers.Count == 0)
        {
            Debug.LogWarning($"HouseGenerator: nie znaleziono rendererów ścian w instancji '{instance.name}'.");
            return;
        }

        foreach (var r in wallRenderers)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            if (HasColorProperty(r.sharedMaterial, "_BaseColor"))
                mpb.SetColor("_BaseColor", c);
            if (HasColorProperty(r.sharedMaterial, "_Color"))
                mpb.SetColor("_Color", c);

            r.SetPropertyBlock(mpb);
        }
    }

    private bool HasColorProperty(Material mat, string prop)
    {
        if (mat == null) return false;
        return mat.HasProperty(prop);
    }

    private GameObject PrefabUtility_Instantiate(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null)
        {
            Debug.LogError("HouseGenerator: housePrefab nie przypisany!");
            return null;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (inst != null)
            {
                inst.transform.position = pos;
                inst.transform.rotation = rot;
            }
            return inst;
        }
#endif
        return Instantiate(prefab, pos, rot);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 a = transform.TransformPoint(new Vector3(-sideOffset, 0f, -streetLength * 0.5f));
        Vector3 b = transform.TransformPoint(new Vector3(-sideOffset, 0f, streetLength * 0.5f));
        Gizmos.DrawLine(a, b);

        Gizmos.color = Color.cyan;
        a = transform.TransformPoint(new Vector3(+sideOffset, 0f, -streetLength * 0.5f));
        b = transform.TransformPoint(new Vector3(+sideOffset, 0f, streetLength * 0.5f));
        Gizmos.DrawLine(a, b);

        Gizmos.color = new Color(1, 0.5f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(transform.TransformPoint(Vector3.zero), new Vector3(sideOffset * 2f + 2f, 0.1f, streetLength));
    }
#endif
}
