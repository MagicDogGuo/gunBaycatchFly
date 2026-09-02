using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace DreamChase.Editor
{
    public static class SpineMecanimSpawner
    {
        const string ButterflyDataPath = "Assets/rYoooProject/Spine/Butterfly/butterfly_SkeletonData.asset";
        const string CatDataPath = "Assets/rYoooProject/Spine/Chiwawa/dog_bg_fg_SkeletonData.asset";
        const string ButterflyPrefabPath = "Assets/Prefabs/Npc/Butterfly.prefab";
        const string CatPrefabPath = "Assets/Prefabs/Player/Cat_Ganbei.prefab";

        [MenuItem("DreamChase/Spawn Spine Placeholders")]
        public static void SpawnPlaceholders()
        {
            Transform charactersRoot = FindTransform("Characters");
            if (charactersRoot == null)
            {
                Debug.LogError("Characters root was not found. Create the DreamChase hierarchy first.");
                return;
            }

            DestroyOrphans();

            GameObject cat = SpawnNamed(
                "Cat_Ganbei",
                CatDataPath,
                charactersRoot,
                new Vector3(-1.5f, 0.95f, 0f),
                Vector3.one * 0.2f,
                "Player",
                "Player");

            GameObject butterfly = SpawnNamed(
                "Butterfly",
                ButterflyDataPath,
                charactersRoot,
                new Vector3(2.2f, 1.7f, 0f),
                Vector3.one * 0.25f,
                "Butterfly",
                "Butterfly");

            SavePrefab(cat, CatPrefabPath);
            SavePrefab(butterfly, ButterflyPrefabPath);
            Debug.Log("Spawned Spine placeholders and saved Cat_Ganbei / Butterfly prefabs.");
        }

        static GameObject SpawnNamed(
            string objectName,
            string skeletonDataPath,
            Transform parent,
            Vector3 worldPosition,
            Vector3 worldScale,
            string tagName,
            string layerName)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataPath);
            if (skeletonData == null)
            {
                Debug.LogError("Missing SkeletonDataAsset at " + skeletonDataPath);
                return null;
            }

            SkeletonMecanim mecanim = Spine.Unity.Editor.EditorInstantiation.InstantiateSkeletonMecanim(skeletonData);
            if (mecanim == null)
            {
                Debug.LogError("Failed to instantiate SkeletonMecanim from " + skeletonDataPath);
                return null;
            }

            GameObject go = mecanim.gameObject;
            go.name = objectName;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            go.transform.localScale = worldScale;

            if (!string.IsNullOrEmpty(tagName))
                go.tag = tagName;

            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                go.layer = layer;

            return go;
        }

        static void DestroyOrphans()
        {
            GameObject leftover = GameObject.Find("New Spine GameObject");
            if (leftover != null)
                Object.DestroyImmediate(leftover);
        }

        static Transform FindTransform(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.transform : null;
        }

        static void SavePrefab(GameObject instance, string path)
        {
            if (instance == null)
                return;

            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
        }
    }
}
