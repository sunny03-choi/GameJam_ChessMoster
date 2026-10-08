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

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("특수 필드 및 상태 테스트", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("샘플 물/장애물 칸 배치"))
                {
                    // 기획서 예시: 물 칸 (중앙 주변 샘플 배치)
                    Tile t1 = manager.GetTile(2, 3);
                    Tile t2 = manager.GetTile(3, 3);
                    Tile t3 = manager.GetTile(4, 3);
                    if (t1 != null) t1.SetTileType(TileType.Water);
                    if (t2 != null) t2.SetTileType(TileType.Water);
                    if (t3 != null) t3.SetTileType(TileType.Water);
                    Debug.Log("[GridManagerEditor] (2,3), (3,3), (4,3) 타일을 '물' 칸으로 설정했습니다.");
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
            EditorGUILayout.LabelField("기획서 명세: 정해진 지형 프리셋", EditorStyles.miniBoldLabel);
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
