using UnityEngine;

namespace ChessMonsters.Grid
{
    /// <summary>
    /// 윗면(Top Face)과 나머지 5면(옆면 4면 + 밑면)을 서로 다른 머티리얼로 렌더링하기 위한
    /// 2-Submesh 정육면체 메시(Cube Mesh) 생성 및 캐싱 유틸리티
    /// - Submesh 0: 옆면 4면 + 밑면 (5면 = 10 삼각형) -> 일반 흙 머티리얼 적용
    /// - Submesh 1: 윗면 (1면 = 2 삼각형) -> 잔디 / 꽃잔디 / 상단 머티리얼 적용
    /// </summary>
    public static class TileMeshUtility
    {
        private static Mesh _cachedMesh;

        public static Mesh GetOrCreateTwoSubmeshCubeMesh()
        {
#if UNITY_EDITOR
            string assetPath = "Assets/Meshes/Tile_Block_TwoSubmesh.asset";
            Mesh assetMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (assetMesh != null)
            {
                return assetMesh;
            }
#endif
            if (_cachedMesh != null) return _cachedMesh;

            _cachedMesh = CreateTwoSubmeshCubeMesh();
#if UNITY_EDITOR
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Meshes"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Meshes");
            }
            UnityEditor.AssetDatabase.CreateAsset(_cachedMesh, "Assets/Meshes/Tile_Block_TwoSubmesh.asset");
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            return _cachedMesh;
        }

        public static Mesh CreateTwoSubmeshCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Tile_Block_TwoSubmesh";

            // 6면에 대해 노멀과 UV가 분리된 24개의 버텍스 생성
            Vector3[] vertices = new Vector3[24];
            Vector3[] normals = new Vector3[24];
            Vector2[] uvs = new Vector2[24];

            // 1. 윗면 (+Y Face) -> Submesh 1 (잔디)
            vertices[0] = new Vector3(-0.5f, 0.5f, -0.5f);
            vertices[1] = new Vector3(-0.5f, 0.5f,  0.5f);
            vertices[2] = new Vector3( 0.5f, 0.5f,  0.5f);
            vertices[3] = new Vector3( 0.5f, 0.5f, -0.5f);
            normals[0] = normals[1] = normals[2] = normals[3] = Vector3.up;
            uvs[0] = new Vector2(0f, 0f);
            uvs[1] = new Vector2(0f, 1f);
            uvs[2] = new Vector2(1f, 1f);
            uvs[3] = new Vector2(1f, 0f);

            // 2. 밑면 (-Y Face) -> Submesh 0 (흙)
            vertices[4] = new Vector3(-0.5f, -0.5f,  0.5f);
            vertices[5] = new Vector3(-0.5f, -0.5f, -0.5f);
            vertices[6] = new Vector3( 0.5f, -0.5f, -0.5f);
            vertices[7] = new Vector3( 0.5f, -0.5f,  0.5f);
            normals[4] = normals[5] = normals[6] = normals[7] = Vector3.down;
            uvs[4] = new Vector2(0f, 0f);
            uvs[5] = new Vector2(0f, 1f);
            uvs[6] = new Vector2(1f, 1f);
            uvs[7] = new Vector2(1f, 0f);

            // 3. 앞/남쪽 (-Z Face) -> Submesh 0 (흙)
            vertices[8]  = new Vector3(-0.5f, -0.5f, -0.5f);
            vertices[9]  = new Vector3(-0.5f,  0.5f, -0.5f);
            vertices[10] = new Vector3( 0.5f,  0.5f, -0.5f);
            vertices[11] = new Vector3( 0.5f, -0.5f, -0.5f);
            normals[8] = normals[9] = normals[10] = normals[11] = Vector3.back;
            uvs[8]  = new Vector2(0f, 0f);
            uvs[9]  = new Vector2(0f, 1f);
            uvs[10] = new Vector2(1f, 1f);
            uvs[11] = new Vector2(1f, 0f);

            // 4. 뒤/북쪽 (+Z Face) -> Submesh 0 (흙)
            vertices[12] = new Vector3( 0.5f, -0.5f, 0.5f);
            vertices[13] = new Vector3( 0.5f,  0.5f, 0.5f);
            vertices[14] = new Vector3(-0.5f,  0.5f, 0.5f);
            vertices[15] = new Vector3(-0.5f, -0.5f, 0.5f);
            normals[12] = normals[13] = normals[14] = normals[15] = Vector3.forward;
            uvs[12] = new Vector2(0f, 0f);
            uvs[13] = new Vector2(0f, 1f);
            uvs[14] = new Vector2(1f, 1f);
            uvs[15] = new Vector2(1f, 0f);

            // 5. 왼쪽/서쪽 (-X Face) -> Submesh 0 (흙)
            vertices[16] = new Vector3(-0.5f, -0.5f,  0.5f);
            vertices[17] = new Vector3(-0.5f,  0.5f,  0.5f);
            vertices[18] = new Vector3(-0.5f,  0.5f, -0.5f);
            vertices[19] = new Vector3(-0.5f, -0.5f, -0.5f);
            normals[16] = normals[17] = normals[18] = normals[19] = Vector3.left;
            uvs[16] = new Vector2(0f, 0f);
            uvs[17] = new Vector2(0f, 1f);
            uvs[18] = new Vector2(1f, 1f);
            uvs[19] = new Vector2(1f, 0f);

            // 6. 오른쪽/동쪽 (+X Face) -> Submesh 0 (흙)
            vertices[20] = new Vector3( 0.5f, -0.5f, -0.5f);
            vertices[21] = new Vector3( 0.5f,  0.5f, -0.5f);
            vertices[22] = new Vector3( 0.5f,  0.5f,  0.5f);
            vertices[23] = new Vector3( 0.5f, -0.5f,  0.5f);
            normals[20] = normals[21] = normals[22] = normals[23] = Vector3.right;
            uvs[20] = new Vector2(0f, 0f);
            uvs[21] = new Vector2(0f, 1f);
            uvs[22] = new Vector2(1f, 1f);
            uvs[23] = new Vector2(1f, 0f);

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;

            // Submesh 0: 밑면 + 옆면 4면 (총 5면 = 10 삼각형 = 30 인덱스) -> 흙
            int[] submesh0 = new int[]
            {
                // 밑면
                4, 5, 6,  4, 6, 7,
                // 남쪽
                8, 9, 10,  8, 10, 11,
                // 북쪽
                12, 13, 14,  12, 14, 15,
                // 서쪽
                16, 17, 18,  16, 18, 19,
                // 동쪽
                20, 21, 22,  20, 22, 23
            };

            // Submesh 1: 윗면 (총 1면 = 2 삼각형 = 6 인덱스) -> 잔디
            int[] submesh1 = new int[]
            {
                0, 1, 2,  0, 2, 3
            };

            mesh.subMeshCount = 2;
            mesh.SetTriangles(submesh0, 0);
            mesh.SetTriangles(submesh1, 1);

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}
