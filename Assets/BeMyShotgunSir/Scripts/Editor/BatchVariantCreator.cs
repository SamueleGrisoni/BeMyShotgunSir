using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using UnityEditor;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Editor
{
    public class BatchVariantCreator
    {
        [MenuItem("Tools/Create Variants and Add Script")]
        public static void CreateVariants()
        {
            // Target save path //Assets\BeMyShotgunSir\Demos\Track Generation\Prefabs\Item\Obstacles
            string targetFolder = "Assets/BeMyShotgunSir/Demos/Track Generation/Prefabs/Item/Obstacles";

            GameObject[] selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

            foreach (GameObject obj in selectedObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab) continue;
                string newPath = $"{targetFolder}/{obj.name}_Variant.prefab";

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(obj);
                //instance.AddComponent<Item>();

                PrefabUtility.SaveAsPrefabAsset(instance, newPath);
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Successfully saved {selectedObjects.Length} variants to {targetFolder}!");
        }
    }
}
