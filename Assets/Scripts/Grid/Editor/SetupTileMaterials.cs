#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChessMonsters.Grid.Editor
{
    public static class SetupTileMaterials
    {
        private const string TexturesDir = "Assets/Textures/Tiles";
        private const string MaterialsDir = "Assets/Materials/Tiles";

        [MenuItem("Chess Monsters/Apply 4 Tile Textures & URP Materials")]
        public static void GenerateMaterialsAndApply()
        {
            EnsureDirectories();

            // 1. 텍스처 임포터 설정 확인 및 텍스처 로드
            Texture2D texDirt = LoadAndConfigureTexture($"{TexturesDir}/Tile_Dirt_01.png");
            Texture2D texDirtGrass = LoadAndConfigureTexture($"{TexturesDir}/Tile_DirtGrass_02.png");
            Texture2D texGrassFlowers = LoadAndConfigureTexture($"{TexturesDir}/Tile_GrassFlowers_03.png");
            Texture2D texGrass = LoadAndConfigureTexture($"{TexturesDir}/Tile_Grass_04.png");

            // 2. URP Lit 셰이더 확인 (분홍색 셰이더 에러 원천 차단)
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (urpShader == null)
            {
                Debug.LogError("[SetupTileMaterials] Universal Render Pipeline 셰이더를 찾을 수 없습니다!");
                return;
            }

            Debug.Log($"[SetupTileMaterials] 사용 셰이더: {urpShader.name}");

            // 3. 4종 타일 및 특수 지형 URP Lit 머티리얼 생성/갱신
            Material matGrass = CreateOrUpdateMaterial(
                $"{MaterialsDir}/Mat_Tile_Grass_04.mat",
                urpShader,
                texGrass,
                Color.white,
                0.1f
            );

            Material matGrassFlowers = CreateOrUpdateMaterial(
                $"{MaterialsDir}/Mat_Tile_GrassFlowers_03.mat",
                urpShader,
                texGrassFlowers,
                Color.white,
                0.1f
            );

            Material matDirt = CreateOrUpdateMaterial(
                $"{MaterialsDir}/Mat_Tile_Dirt_01.mat",
                urpShader,
                texDirt,
                Color.white,
                0.1f
            );

            Material matDirtGrass = CreateOrUpdateMaterial(
                $"{MaterialsDir}/Mat_Tile_DirtGrass_02.mat",
                urpShader,
                texDirtGrass,
                Color.white,
                0.1f
            );

            Material matWater = CreateOrUpdateMaterial(
                $"{MaterialsDir}/Mat_Tile_Water.mat",
                urpShader,
                null,
                new Color(0.20f, 0.60f, 0.90f, 0.95f),
                0.85f
            );

            Material matObstacle = CreateOrUpdateMaterial(
                $"{MaterialsDir}/Mat_Tile_Obstacle.mat",
                urpShader,
                texDirt,
                new Color(0.35f, 0.35f, 0.35f, 1f),
                0.25f
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. 2-Submesh 큐브 메시 에셋 생성 및 확인
            Mesh cubeMesh = TileMeshUtility.GetOrCreateTwoSubmeshCubeMesh();
            Debug.Log($"[SetupTileMaterials] 2-Submesh 큐브 메시 준비 완료: {cubeMesh.name} (Submesh 개수: {cubeMesh.subMeshCount})");

            // 5. 씬 내 GridManager에 머티리얼 적용 및 7x7 그리드 재생성
            GridManager gm = Object.FindFirstObjectByType<GridManager>();
            if (gm == null)
            {
                // 없으면 새로 생성
                SetupChessBoardScene.SetupScene();
                gm = Object.FindFirstObjectByType<GridManager>();
            }

            if (gm != null)
            {
                SerializedObject so = new SerializedObject(gm);
                so.FindProperty("_lightTileMaterial").objectReferenceValue = matGrass;
                so.FindProperty("_lightTileVariationMaterial").objectReferenceValue = matGrassFlowers;
                so.FindProperty("_darkTileMaterial").objectReferenceValue = matDirt;
                so.FindProperty("_darkTileVariationMaterial").objectReferenceValue = matDirtGrass;
                so.FindProperty("_waterMaterial").objectReferenceValue = matWater;
                so.FindProperty("_obstacleMaterial").objectReferenceValue = matObstacle;

                // 정육면체 (1.0f) 및 랜덤 잔디 배치 기본 모드 적용
                so.FindProperty("_tileThickness").floatValue = 1.0f;
                so.FindProperty("_placementMode").enumValueIndex = (int)TilePlacementMode.RandomGrass;
                so.FindProperty("_randomizeRotation").boolValue = true;

                so.ApplyModifiedProperties();

                // 7x7 그리드 생성
                gm.GenerateGrid();
                EditorUtility.SetDirty(gm);

                // 프리팹 생성/저장
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                string prefabPath = "Assets/Prefabs/ChessBoard.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(gm.gameObject, prefabPath, InteractionMode.AutomatedAction);

                // 씬 저장
                Scene activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);

                Debug.Log($"[SetupTileMaterials] 4종 텍스처 머티리얼 적용 및 7x7 체스판 프리팹/씬 갱신 완료!");
            }
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Materials/Tiles"))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Tiles");
            }
        }

        private static Texture2D LoadAndConfigureTexture(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool needReimport = false;
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    needReimport = true;
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    needReimport = true;
                }
                if (needReimport)
                {
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Material CreateOrUpdateMaterial(string matPath, Shader shader, Texture2D texture, Color color, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            if (texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTexture("_MainTex", texture);
            }

            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void SetupAllBatch()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath);
            GenerateMaterialsAndApply();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupTileMaterials Batch] 텍스처 적용 및 씬 배치 실행 완료.");
        }
    }
}
#endif
