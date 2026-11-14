#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChessMonsters.Grid.Editor
{
    /// <summary>
    /// 필드 내 신규 리소스(나무 다리, 3프레임 애니메이션 물, 외곽 석벽, 진영 깃발) 절차적 생성기
    /// </summary>
    public static class CreateTerrainTextures
    {
        private const string TileTexturesDir = "Assets/Textures/Tiles";
        private const string UiTexturesDir = "Assets/Textures/UI";
        private const string MaterialsDir = "Assets/Materials/Tiles";

        public const string BridgeTexturePath = TileTexturesDir + "/Tile_Bridge_Wood.png";
        public const string Water01Path = TileTexturesDir + "/Tile_Water_01.png";
        public const string Water02Path = TileTexturesDir + "/Tile_Water_02.png";
        public const string Water03Path = TileTexturesDir + "/Tile_Water_03.png";
        public const string WallTexturePath = TileTexturesDir + "/Tile_Wall_Stone.png";
        public const string BlueBannerPath = UiTexturesDir + "/Sprite_Banner_Blue.png";
        public const string RedBannerPath = UiTexturesDir + "/Sprite_Banner_Red.png";

        public const string BridgeMatPath = MaterialsDir + "/Mat_Tile_Bridge_Wood.mat";
        public const string WaterMatPath = MaterialsDir + "/Mat_Tile_Water.mat";
        public const string WallMatPath = MaterialsDir + "/Mat_Tile_Wall_Stone.mat";

        [MenuItem("Chess Monsters/Generate Terrain Textures & Materials")]
        public static void GenerateAll()
        {
            EnsureDirectories();

            // 1. 텍스처 절차적 생성
            CreateWoodBridgeTexture();
            CreateWaterFrameTexture(Water01Path, 0.0f);
            CreateWaterFrameTexture(Water02Path, 0.333f);
            CreateWaterFrameTexture(Water03Path, 0.666f);
            CreateCobblestoneTexture();
            CreateBannerSprite(BlueBannerPath, new Color(0.12f, 0.35f, 0.85f), new Color(0.25f, 0.55f, 0.95f));
            CreateBannerSprite(RedBannerPath, new Color(0.85f, 0.18f, 0.18f), new Color(0.98f, 0.35f, 0.35f));

            AssetDatabase.Refresh();

            // 2. 텍스처 임포터 설정
            ConfigureTileTexture(BridgeTexturePath);
            ConfigureTileTexture(Water01Path);
            ConfigureTileTexture(Water02Path);
            ConfigureTileTexture(Water03Path);
            ConfigureTileTexture(WallTexturePath);

            ConfigureSpriteTexture(BlueBannerPath);
            ConfigureSpriteTexture(RedBannerPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. URP 머티리얼 생성 및 바인딩
            CreateOrUpdateTerrainMaterials();

            Debug.Log("[CreateTerrainTextures] 나무 다리, 3프레임 물, 석벽, 진영 깃발 리소스 및 머티리얼 생성 완료!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(TileTexturesDir))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "Tiles");
            }
            if (!AssetDatabase.IsValidFolder(UiTexturesDir))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "UI");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsDir))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Tiles");
            }
        }

        /// <summary>
        /// 나무 판자 다리 텍스처 절차적 생성 (가로 판자 4줄, 나뭇결, 판자 경계선, 대갈못/못 디테일)
        /// </summary>
        private static void CreateWoodBridgeTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            int plankCount = 4;
            float plankHeight = (float)size / plankCount;

            Color baseWood = new Color(0.58f, 0.38f, 0.22f);      // 따뜻한 오크/스프루스 원목
            Color darkGrain = new Color(0.44f, 0.27f, 0.14f);     // 나뭇결 음영
            Color lightGrain = new Color(0.68f, 0.46f, 0.28f);    // 나뭇결 하이라이트
            Color seamColor = new Color(0.20f, 0.12f, 0.06f);     // 판자 틈새 어두운 선
            Color nailColor = new Color(0.22f, 0.22f, 0.24f);     // 철제 못

            for (int y = 0; y < size; y++)
            {
                int plankIndex = Mathf.Clamp(Mathf.FloorToInt(y / plankHeight), 0, plankCount - 1);
                float localY = y - (plankIndex * plankHeight);
                float normalizedY = localY / plankHeight;

                for (int x = 0; x < size; x++)
                {
                    // 1. 기본 나뭇결 (가로 방향 파동 + 미세 노이즈)
                    float grainNoise = Mathf.Sin(x * 0.08f + plankIndex * 13.5f) * 0.5f +
                                       Mathf.Cos((x + y * 2) * 0.15f) * 0.25f +
                                       Mathf.PerlinNoise(x * 0.05f, (y + plankIndex * 50) * 0.05f) * 0.5f;

                    Color pixelColor = Color.Lerp(baseWood, (grainNoise > 0.55f ? lightGrain : darkGrain), Mathf.Abs(grainNoise - 0.5f) * 1.5f);

                    // 2. 판자 상단/하단 베벨 (입체감)
                    if (normalizedY < 0.12f)
                    {
                        // 하단 그림자
                        pixelColor = Color.Lerp(seamColor, pixelColor, normalizedY / 0.12f);
                    }
                    else if (normalizedY > 0.88f)
                    {
                        // 상단 하이라이트
                        pixelColor = Color.Lerp(pixelColor, lightGrain * 1.15f, (normalizedY - 0.88f) / 0.12f);
                    }

                    // 3. 판자 간 깊은 이음매(Seam) 경계선 (y % plankHeight == 0 부근)
                    if (localY < 2.5f || localY > plankHeight - 2.5f)
                    {
                        pixelColor = seamColor;
                    }

                    // 4. 좌우 끝단 철제 못(Nail) 디테일 (x = 22 또는 x = size - 22)
                    float nailCenterY = (plankIndex + 0.5f) * plankHeight;
                    float distNailLeft = Vector2.Distance(new Vector2(x, y), new Vector2(24f, nailCenterY));
                    float distNailRight = Vector2.Distance(new Vector2(x, y), new Vector2(size - 24f, nailCenterY));

                    if (distNailLeft < 5.0f || distNailRight < 5.0f)
                    {
                        float d = Mathf.Min(distNailLeft, distNailRight);
                        if (d < 3.2f)
                        {
                            pixelColor = nailColor;
                        }
                        else
                        {
                            pixelColor = seamColor; // 못 외곽선
                        }
                    }

                    pixels[y * size + x] = pixelColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(BridgeTexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 3프레임 순환 애니메이션 물 텍스처 프레임 절차적 생성
        /// (물결 코스틱, 잔물결 하이라이트 파동 위상 오프셋 적용)
        /// </summary>
        private static void CreateWaterFrameTexture(string savePath, float phaseOffset)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Color deepWater = new Color(0.12f, 0.40f, 0.82f, 0.95f);     // 맑고 깊은 청록빛 물
            Color midWater = new Color(0.18f, 0.55f, 0.92f, 0.95f);      // 중간 수면
            Color waveHighlight = new Color(0.55f, 0.85f, 1.0f, 0.98f);  // 물결 하이라이트
            Color foamCrest = new Color(0.88f, 0.96f, 1.0f, 1.0f);       // 물거품/포말

            float phase = phaseOffset * Mathf.PI * 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // 흐르는 대각선 물결 파동 합성
                    float wave1 = Mathf.Sin((u * 4f + v * 3f) * Mathf.PI * 2f + phase);
                    float wave2 = Mathf.Cos((u * 6f - v * 4f) * Mathf.PI * 2f + phase * 1.3f);
                    float wave3 = Mathf.Sin((u * 8f + v * 8f) * Mathf.PI * 2f - phase * 0.7f);

                    float ripple = (wave1 * 0.5f + wave2 * 0.35f + wave3 * 0.15f);

                    Color c = Color.Lerp(deepWater, midWater, (ripple + 1f) * 0.5f);

                    // 물결 첨두(Crest) 포말 및 빛 반사
                    if (ripple > 0.55f)
                    {
                        float t = (ripple - 0.55f) / 0.45f;
                        c = Color.Lerp(c, waveHighlight, t);
                        if (ripple > 0.82f)
                        {
                            c = Color.Lerp(c, foamCrest, (ripple - 0.82f) / 0.18f);
                        }
                    }

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(savePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 외곽 석벽(Cobblestone) 텍스처 절차적 생성 (모루/돌 블록과 어두운 시멘트 줄눈)
        /// </summary>
        private static void CreateCobblestoneTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Color stoneBase = new Color(0.48f, 0.48f, 0.50f);
            Color stoneDark = new Color(0.35f, 0.35f, 0.37f);
            Color stoneLight = new Color(0.62f, 0.62f, 0.65f);
            Color mortar = new Color(0.20f, 0.20f, 0.22f);

            for (int y = 0; y < size; y++)
            {
                int row = y / 64;
                float localY = y % 64;
                int xOffset = (row % 2 == 1) ? 32 : 0;

                for (int x = 0; x < size; x++)
                {
                    int col = (x + xOffset) / 64;
                    float localX = (x + xOffset) % 64;

                    // 블록 모서리 줄눈
                    bool isMortar = (localX < 3.5f || localX > 60.5f || localY < 3.5f || localY > 60.5f);

                    if (isMortar)
                    {
                        pixels[y * size + x] = mortar;
                    }
                    else
                    {
                        float noise = Mathf.PerlinNoise((x + row * 100) * 0.1f, (y + col * 100) * 0.1f);
                        Color c = Color.Lerp(stoneDark, stoneLight, noise);
                        pixels[y * size + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(WallTexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 팀 진영 깃발 2D 빌보드 스프라이트 (나무 깃대 + 펄럭이는 천 깃발)
        /// </summary>
        private static void CreateBannerSprite(string path, Color bannerBase, Color bannerHighlight)
        {
            int w = 128;
            int h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color poleColor = new Color(0.35f, 0.22f, 0.12f);
            Color goldFinial = new Color(0.95f, 0.80f, 0.20f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.clear;

                    // 1. 중앙 나무 깃대 (x: 20 ~ 28, y: 10 ~ 240)
                    if (x >= 20 && x <= 28 && y >= 10 && y <= 240)
                    {
                        c = poleColor;
                        if (x == 20 || x == 28) c *= 0.7f;
                    }

                    // 2. 깃대 꼭대기 골드 장식구 (Finial)
                    float distFinial = Vector2.Distance(new Vector2(x, y), new Vector2(24f, 244f));
                    if (distFinial <= 9f)
                    {
                        c = goldFinial;
                        if (distFinial > 7f) c *= 0.8f;
                    }

                    // 3. 천 깃발 (x: 28 ~ 116, y: 130 ~ 235)
                    if (x > 28 && x <= 116 && y >= 130 && y <= 235)
                    {
                        // 깃발 하단 삼각형 컷아웃 (제비꼬리 깃발)
                        float relX = (float)(x - 28) / (116 - 28);
                        float bottomCut = 130f + (1f - Mathf.Abs(relX - 0.5f) * 2f) * 20f;

                        if (y >= bottomCut)
                        {
                            // 펄럭이는 명암
                            float fold = Mathf.Sin(relX * Mathf.PI * 3f);
                            c = Color.Lerp(bannerBase, bannerHighlight, (fold + 1f) * 0.5f);

                            // 깃발 외곽 테두리
                            if (x == 116 || y == 235 || Mathf.Abs(y - bottomCut) < 2f)
                            {
                                c = Color.Lerp(c, goldFinial, 0.7f);
                            }
                        }
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureTileTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureSpriteTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128f;
                importer.filterMode = FilterMode.Bilinear;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0.0f);
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }
        }

        private static void CreateOrUpdateTerrainMaterials()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Standard");

            Texture2D texBridge = AssetDatabase.LoadAssetAtPath<Texture2D>(BridgeTexturePath);
            Texture2D texWater = AssetDatabase.LoadAssetAtPath<Texture2D>(Water01Path);
            Texture2D texWall = AssetDatabase.LoadAssetAtPath<Texture2D>(WallTexturePath);

            // 1. 나무 다리 머티리얼
            Material matBridge = AssetDatabase.LoadAssetAtPath<Material>(BridgeMatPath);
            if (matBridge == null)
            {
                matBridge = new Material(urpShader);
                AssetDatabase.CreateAsset(matBridge, BridgeMatPath);
            }
            matBridge.shader = urpShader;
            if (texBridge != null)
            {
                matBridge.SetTexture("_BaseMap", texBridge);
                matBridge.SetTexture("_MainTex", texBridge);
            }
            matBridge.SetColor("_BaseColor", Color.white);
            matBridge.SetFloat("_Smoothness", 0.15f);

            // 2. 물 머티리얼 (3프레임 텍스처 순환용 기본 머티리얼)
            Material matWater = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
            if (matWater == null)
            {
                matWater = new Material(urpShader);
                AssetDatabase.CreateAsset(matWater, WaterMatPath);
            }
            matWater.shader = urpShader;
            if (texWater != null)
            {
                matWater.SetTexture("_BaseMap", texWater);
                matWater.SetTexture("_MainTex", texWater);
            }
            matWater.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.95f));
            matWater.SetFloat("_Smoothness", 0.90f);

            // 3. 외곽 석벽 머티리얼
            Material matWall = AssetDatabase.LoadAssetAtPath<Material>(WallMatPath);
            if (matWall == null)
            {
                matWall = new Material(urpShader);
                AssetDatabase.CreateAsset(matWall, WallMatPath);
            }
            matWall.shader = urpShader;
            if (texWall != null)
            {
                matWall.SetTexture("_BaseMap", texWall);
                matWall.SetTexture("_MainTex", texWall);
            }
            matWall.SetColor("_BaseColor", Color.white);
            matWall.SetFloat("_Smoothness", 0.15f);

            EditorUtility.SetDirty(matBridge);
            EditorUtility.SetDirty(matWater);
            EditorUtility.SetDirty(matWall);
            AssetDatabase.SaveAssets();
        }

        public static void GenerateAllBatch()
        {
            GenerateAll();
        }
    }
}
#endif