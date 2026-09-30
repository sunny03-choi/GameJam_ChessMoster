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
                Undo.RegisterCreatedObjectUndo(boardObj, "Create ChessBoard");
            }
            else
            {
                boardObj = gridManager.gameObject;
                if (boardObj.GetComponent<GridInteractionHandler>() == null)
                {
                    boardObj.AddComponent<GridInteractionHandler>();
                }
            }

            boardObj.transform.position = Vector3.zero;
            boardObj.transform.rotation = Quaternion.identity;

            // 3. 7x7 그리드 타일 생성
            gridManager.GenerateGrid();

            // 4. 메인 카메라를 기획서의 아이소메트릭(45도 다이아몬드) 각도로 배치
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();
            if (mainCam != null)
            {
                Undo.RecordObject(mainCam.transform, "Adjust Camera For Chessboard");
                mainCam.transform.position = new Vector3(-6.8f, 9.0f, -6.8f);
                mainCam.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
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
                dirLight.intensity = 1.1f;
            }

            // 6. 씬 저장
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
