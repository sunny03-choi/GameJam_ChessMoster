#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChessMonsters.Grid.Editor
{
    public static class SetupChessBoardScene
    {
        [MenuItem("Chess Monsters/Setup Scene & Board")]
        public static void SetupScene()
        {
            // 1. 활성 씬 확인
            Scene scene = SceneManager.GetActiveScene();

            // 2. ChessBoard 게임 오브젝트 찾기 또는 생성
            GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
            GameObject boardObj;

            if (gridManager == null)
            {
                boardObj = new GameObject("ChessBoard");
                gridManager = boardObj.AddComponent<GridManager>();
                boardObj.AddComponent<GridInteractionHandler>();
                boardObj.AddComponent<WaterTileAnimator>();
                Undo.RegisterCreatedObjectUndo(boardObj, "Create ChessBoard");
            }
            else
            {
                boardObj = gridManager.gameObject;
                if (boardObj.GetComponent<GridInteractionHandler>() == null)
                {
                    boardObj.AddComponent<GridInteractionHandler>();
                }
                if (boardObj.GetComponent<WaterTileAnimator>() == null)
                {
                    boardObj.AddComponent<WaterTileAnimator>();
                }
            }

            boardObj.transform.position = Vector3.zero;
            boardObj.transform.rotation = Quaternion.identity;

            // 3. 신규 리소스(다리, 3프레임 물, 석벽) 생성 및 9x9 그리드 타일 생성
            CreateTerrainTextures.GenerateAll();
            gridManager.SetupDefaultMaterialsIfNull();
            gridManager.GenerateGrid();
            gridManager.ApplyFieldPreset(FieldPresetType.BattleIsland);

            // 4. 9x9 전장 전체와 외곽 석벽을 한눈에 담도록 카메라 위치 조정 (30도 아이소메트릭)
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();
            if (mainCam != null)
            {
                Undo.RecordObject(mainCam.transform, "Adjust Camera For 9x9 Chessboard");
                mainCam.transform.position = new Vector3(-8.2f, 7.0f, -8.2f);
                mainCam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
                mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.18f); // 짙은 네이비/차콜 배경
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }

            // 5. 조명(Directional Light) 조정
            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                Undo.RecordObject(dirLight.transform, "Adjust Light");
                dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                dirLight.color = new Color(1f, 0.98f, 0.95f);
                dirLight.intensity = 1.15f;
            }

            // 6. 2D 빌보드 샘플 유닛 배치 (레드 진영 중앙 다리 앞 타일 4, 3)
            Tile startTile = gridManager.GetTile(4, 3);
            if (startTile != null && startTile.OccupyingUnit == null)
            {
                GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Unit.prefab");
                if (unitPrefab != null)
                {
                    GameObject unitObj = PrefabUtility.InstantiatePrefab(unitPrefab) as GameObject;
                    if (unitObj != null)
                    {
                        Undo.RegisterCreatedObjectUndo(unitObj, "Spawn Initial Unit");
                        Unit.Unit unit = unitObj.GetComponent<Unit.Unit>();
                        if (unit != null) unit.PlaceOnTile(startTile);
                    }
                }
            }

            // 7. 씬 저장
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Chess Monsters] 체스판 그리드 및 씬 셋업이 완료되었습니다!");
        }

        public static void SetupSceneBatch()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath);
            SetupScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Chess Monsters Batch] SampleScene 셋업 및 저장 완료.");
        }
    }
}
#endif
