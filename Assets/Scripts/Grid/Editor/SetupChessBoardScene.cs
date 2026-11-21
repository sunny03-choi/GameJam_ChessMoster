#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChessMonsters.UI;
using ChessMonsters.UI.Editor;
using ChessMonsters.Unit;

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
                boardObj.AddComponent<UnitActionMenuUI>();
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
                if (boardObj.GetComponent<UnitActionMenuUI>() == null)
                {
                    boardObj.AddComponent<UnitActionMenuUI>();
                }
            }

            boardObj.transform.position = Vector3.zero;
            boardObj.transform.rotation = Quaternion.identity;

            // 3. 신규 리소스(다리, 3프레임 물, 석벽, 쿠키런 3종 선택지 텍스처) 생성 및 9x9 그리드 타일 생성
            CreateTerrainTextures.GenerateAll();
            CreateActionMenuTextures.GenerateAll();
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

            // 6. 2D 빌보드 샘플 유닛 배치 (다리 4,4를 사이에 두고 아군 4,3 vs 적군 4,5 대치)
            GameObject unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Unit.prefab");
            if (unitPrefab != null)
            {
                // 6-1. 아군 폰 (White Pawn, 4, 3)
                Tile playerTile = gridManager.GetTile(4, 3);
                if (playerTile != null)
                {
                    if (playerTile.OccupyingUnit != null) Object.DestroyImmediate(playerTile.OccupyingUnit);
                    GameObject playerObj = PrefabUtility.InstantiatePrefab(unitPrefab) as GameObject;
                    if (playerObj != null)
                    {
                        playerObj.name = "Unit_Player_Pawn";
                        Undo.RegisterCreatedObjectUndo(playerObj, "Spawn Player Unit");
                        Unit.Unit pUnit = playerObj.GetComponent<Unit.Unit>();
                        if (pUnit != null)
                        {
                            pUnit.Team = UnitTeam.Player;
                            pUnit.UnitName = "Pawn";
                            pUnit.PlaceOnTile(playerTile);
                        }
                    }
                }

                // 6-2. 적군 몬스터 폰 (Red Pawn, 4, 5 - 다리 건너편, 사거리 2 내)
                Tile enemyTile = gridManager.GetTile(4, 5);
                if (enemyTile != null)
                {
                    if (enemyTile.OccupyingUnit != null) Object.DestroyImmediate(enemyTile.OccupyingUnit);
                    GameObject enemyObj = PrefabUtility.InstantiatePrefab(unitPrefab) as GameObject;
                    if (enemyObj != null)
                    {
                        enemyObj.name = "Unit_Enemy_Pawn";
                        Undo.RegisterCreatedObjectUndo(enemyObj, "Spawn Enemy Unit");
                        Unit.Unit eUnit = enemyObj.GetComponent<Unit.Unit>();
                        if (eUnit != null)
                        {
                            eUnit.Team = UnitTeam.Enemy;
                            eUnit.UnitName = "Enemy Pawn";
                            eUnit.PlaceOnTile(enemyTile);
                        }
                    }
                }
            }

            // 7. 씬 저장
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Chess Monsters] 쿠키런 3종 선택지 및 아군/적군 대치 구도 셋업 완료!");
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
