using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using UnityEditor;
using UnityEngine;

namespace BeMyShotgunSir.Editor
{
    public class BatchVariantCreator
    {
        [MenuItem("Tools/Create Variants and Add Script")]
        public static void CreateVariants()
        {
            // Target save path
            string targetFolder = "Assets/BeMyShotgunSir/Demos/Track Generation/Prefabs/Props";

            GameObject[] selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

            foreach (GameObject obj in selectedObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab) continue;
                string newPath = $"{targetFolder}/{obj.name}_Variant.prefab";

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(obj);
                instance.AddComponent<CityProps>();

                PrefabUtility.SaveAsPrefabAsset(instance, newPath);
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Successfully saved {selectedObjects.Length} variants to {targetFolder}!");
        }
    }
}
