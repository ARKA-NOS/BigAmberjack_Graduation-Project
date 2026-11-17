using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyBloodTrails
{
    public static async Task<object> RunPlay()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != "")
            throw new Exception("Use an isolated empty Play scene");

        string[] paths =
        {
            "Assets/_Prefabs/Enemies/Effects/HitBloodSpray.prefab",
            "Assets/_Prefabs/Enemies/Effects/DeathBloodBurst.prefab"
        };
        var results = new object[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab == null) throw new Exception("Missing blood prefab: " + paths[i]);
            var instance = Object.Instantiate(prefab, new Vector3(i * 5f, 2f), Quaternion.identity);
            try
            {
                instance.GetComponent<EnemyHitParticle>().enabled = false;
                typeof(EnemyBloodParticle).GetMethod("Emit", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(instance.GetComponent<EnemyBloodParticle>(), null);
                int maxVertices = 0, maxTriangles = 0;
                float maxAlpha = 0f;
                for (int frame = 0; frame < 50; frame++)
                {
                    await Awaitable.NextFrameAsync();
                    var filter = instance.transform.Find("Blood Trails")?.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    Mesh mesh = filter.sharedMesh;
                    maxVertices = Mathf.Max(maxVertices, mesh.vertexCount);
                    maxTriangles = Mathf.Max(maxTriangles, mesh.triangles.Length / 3);
                    foreach (Color color in mesh.colors) maxAlpha = Mathf.Max(maxAlpha, color.a);
                }
                if (maxVertices == 0 || maxTriangles == 0 || maxAlpha <= 0f || maxAlpha > 0.081f)
                    throw new Exception($"Trail check failed for {paths[i]}: {maxVertices} vertices, {maxTriangles} triangles, alpha {maxAlpha}");
                results[i] = new { prefab = paths[i], maxVertices, maxTriangles, maxAlpha,
                    material = instance.transform.Find("Blood Trails").GetComponent<MeshRenderer>().sharedMaterial.name };
            }
            finally { Object.Destroy(instance); }
        }
        return results;
    }
}
