#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChessMonsters.Grid.Editor
{
    [CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridManager manager = (GridManager)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("체스판 그리드 에디터 도구", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.4f, 0.85f, 0.5f);
                if (GUILayout.Button("7x7 그리드 생성 (Generate)", GUILayout.Height(32)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Generate 7x7 Grid");
                    manager.GenerateGrid();
                    EditorUtility.SetDirty(manager);
                }

                GUI.backgroundColor = new Color(1.0f, 0.45f, 0.45f);
                if (GUILayout.Button("그리드 초기화 (Clear)", GUILayout.Height(32)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Clear Grid");
                    manager.ClearGrid();
                    EditorUtility.SetDirty(manager);
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("잔디 배치 및 정육면체 큐브 설정", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.45f, 0.95f, 0.45f);
                if (GUILayout.Button("🎲 잔디 랜덤 재배치 (Randomize)", GUILayout.Height(30)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Randomize Grass Placement");
                    manager.RandomizeGrassPlacement();
                    EditorUtility.SetDirty(manager);
                }

                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.5f);
                if (GUILayout.Button("🏁 체스판 패턴으로 배치", GUILayout.Height(30)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Set Checkerboard Placement");
                    manager.SetCheckerboardPlacement();
                    EditorUtility.SetDirty(manager);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = manager.TileThickness >= 0.9f ? new Color(0.35f, 0.85f, 1.0f) : Color.white;
                if (GUILayout.Button("📦 정육면체 큐브 (1x1x1)", GUILayout.Height(26)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Set Cube Thickness");
                    manager.SetTileThickness(1.0f);
                    EditorUtility.SetDirty(manager);
                }

                GUI.backgroundColor = manager.TileThickness < 0.9f ? new Color(0.35f, 0.85f, 1.0f) : Color.white;
                if (GUILayout.Button("📏 납작한 슬랩 (1x0.3x1)", GUILayout.Height(26)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Set Slab Thickness");
                    manager.SetTileThickness(0.3f);
                    EditorUtility.SetDirty(manager);
                }
            }

            GUI.backgroundColor = new Color(0.3f, 0.75f, 1.0f);
            if (GUILayout.Button("🎨 타일 텍스처 머티리얼 일괄 적용 (URP)", GUILayout.Height(30)))
            {
                SetupTileMaterials.GenerateMaterialsAndApply();
            }

            GUI.backgroundColor = new Color(1.0f, 0.95f, 0.4f);
            if (GUILayout.Button("♟ 샘플 2D 빌보드 유닛 소환 (4,3)", GUILayout.Height(30)))
            {
                Tile centerTile = manager.GetTile(4, 3);
                if (centerTile != null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Unit.prefab");
                    if (prefab != null)
                    {
                        if (centerTile.OccupyingUnit != null)
                        {
                            DestroyImmediate(centerTile.OccupyingUnit);
                            centerTile.SetOccupyingUnit(null);
                        }

                        GameObject unitObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (unitObj != null)
                        {
                            Undo.RegisterCreatedObjectUndo(unitObj, "Spawn Sample Unit");
                            Unit.Unit unit = unitObj.GetComponent<Unit.Unit>();
                            if (unit != null) unit.PlaceOnTile(centerTile);
                            Selection.activeGameObject = unitObj;
                            Debug.Log("[GridManagerEditor] (4,3) 타일에 샘플 2D 빌보드 유닛 소환 완료!");
                        }
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("특수 필드 및 상태 테스트", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("샘플 물/나무다리 칸 배치"))
                {
                    Tile t1 = manager.GetTile(3, 4);
                    Tile t2 = manager.GetTile(4, 4);
                    Tile t3 = manager.GetTile(5, 4);
                    if (t1 != null) t1.SetTileType(TileType.Water);
                    if (t2 != null) t2.SetTileType(TileType.Bridge);
                    if (t3 != null) t3.SetTileType(TileType.Water);
                    Debug.Log("[GridManagerEditor] (3,4) 물, (4,4) 나무다리, (5,4) 물 칸으로 설정했습니다.");
                }

                if (GUILayout.Button("랜덤 타일 파괴 테스트"))
                {
                    int rx = Random.Range(0, GridManager.BoardWidth);
                    int rz = Random.Range(0, GridManager.BoardHeight);
                    manager.DestroyTile(rx, rz, true);
                }
            }

            if (GUILayout.Button("모든 타일 일반 복구"))
            {
                for (int z = 0; z < GridManager.BoardHeight; z++)
                {
                    for (int x = 0; x < GridManager.BoardWidth; x++)
                    {
                        manager.CreateTile(x, z, TileType.Normal, false);
                    }
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("지형 프리셋 (참고 사진 지형 포함)", EditorStyles.miniBoldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.85f, 1.0f);
            if (GUILayout.Button("⚔️ 5. 9x9 대격돌 섬 전장 (참고 사진 지형)", GUILayout.Height(32)))
            {
                Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Apply BattleIsland Preset");
                manager.ApplyFieldPreset(FieldPresetType.BattleIsland);
                EditorUtility.SetDirty(manager);
            }
            GUI.backgroundColor = Color.white;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("1. 기본 평원"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Apply Default Plain Preset");
                    manager.ApplyFieldPreset(FieldPresetType.DefaultPlain);
                    EditorUtility.SetDirty(manager);
                }
                if (GUILayout.Button("2. 강/호수 지형"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Apply River Preset");
                    manager.ApplyFieldPreset(FieldPresetType.RiverField);
                    EditorUtility.SetDirty(manager);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("3. 고지대 단차 지형"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Apply Highland Preset");
                    manager.ApplyFieldPreset(FieldPresetType.HighlandField);
                    EditorUtility.SetDirty(manager);
                }
                if (GUILayout.Button("4. 습지 지형"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Apply Marsh Preset");
                    manager.ApplyFieldPreset(FieldPresetType.MarshField);
                    EditorUtility.SetDirty(manager);
                }
            }
        }
    }
}
#endif
