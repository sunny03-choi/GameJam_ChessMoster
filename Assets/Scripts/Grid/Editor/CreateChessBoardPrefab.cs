#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChessMonsters.Grid.Editor
{
    public static class CreateChessBoardPrefab
    {
        [MenuItem("Chess Monsters/Create ChessBoard Prefab")]
        public static void CreatePrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            GridManager gm = Object.FindFirstObjectByType<GridManager>();
            if (gm != null)
            {
                string prefabPath = "Assets/Prefabs/ChessBoard.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(gm.gameObject, prefabPath, InteractionMode.AutomatedAction);
                Debug.Log($"[Chess Monsters] Prefab saved successfully at {prefabPath}");
            }
        }

        public static void CreatePrefabBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            CreatePrefab();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
        }
    }
}
#endif
