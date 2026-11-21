#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using ChessMonsters.Grid;
using ChessMonsters.UI.Editor;

namespace ChessMonsters.Unit.Editor
{
    public static class CreateUnitTexturesAndPrefab
    {
        private const string TexturesDir = "Assets/Textures/Units";
        private const string UiTexturesDir = "Assets/Textures/UI";
        private const string PrefabPath = "Assets/Prefabs/Unit.prefab";
        private const string PawnSpritePath = TexturesDir + "/Sprite_Unit_Pawn_White.png";
        private const string ShadowSpritePath = TexturesDir + "/Sprite_Unit_Shadow.png";
        private const string SelectionRingSpritePath = TexturesDir + "/Sprite_Unit_SelectionRing.png";
        public const string MoveBorderSpritePath = UiTexturesDir + "/Sprite_Move_Border.png";

        [MenuItem("Chess Monsters/Create Unit Prefab & Textures")]
        public static void GenerateAll()
        {
            EnsureDirectories();

            // 1. 2D 텍스처들 절차적 생성
            CreatePawnSpriteTexture();
            CreateShadowTexture();
            CreateSelectionRingTexture();
            CreateMoveBorderTexture();
            CreateActionMenuTextures.GenerateAll();

            AssetDatabase.Refresh();

            // 2. TextureImporter 설정 (Sprite 모드, 바닥 피벗)
            ConfigureSpriteImporter(PawnSpritePath, new Vector2(0.5f, 0.0f), 256f);
            ConfigureSpriteImporter(ShadowSpritePath, new Vector2(0.5f, 0.5f), 128f);
            ConfigureSpriteImporter(SelectionRingSpritePath, new Vector2(0.5f, 0.5f), 128f);
            ConfigureSpriteImporter(MoveBorderSpritePath, new Vector2(0.5f, 0.5f), 128f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. 유닛 프리팹(Unit.prefab) 조립 및 저장
            CreatePrefab();

            Debug.Log("[CreateUnitTexturesAndPrefab] 흰색 유닛 스프라이트, 이동 경계선 및 3D 빌보드 유닛 프리팹 생성 완료!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(TexturesDir))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "Units");
            }
            if (!AssetDatabase.IsValidFolder(UiTexturesDir))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "UI");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        private static void CreatePawnSpriteTexture()
        {
            int width = 256;
            int height = 384;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2(width * 0.5f, 0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x;
                    float py = y;
                    float dx = Mathf.Abs(px - center.x);

                    float alpha = 0f;
                    float shade = 1.0f;

                    // 1. 머리 구체 (Head)
                    float headDist = Vector2.Distance(new Vector2(px, py), new Vector2(128f, 285f));
                    if (headDist <= 46f)
                    {
                        alpha = Mathf.Clamp01((46f - headDist) * 1.5f);
                        float lightDist = Vector2.Distance(new Vector2(px, py), new Vector2(115f, 298f));
                        shade = Mathf.Lerp(1.0f, 0.82f, lightDist / 50f);
                    }

                    // 2. 목 링 (Neck Collar)
                    if (py >= 226f && py <= 244f)
                    {
                        float collarW = 34f;
                        if (dx <= collarW)
                        {
                            float a = Mathf.Clamp01((collarW - dx) * 1.5f);
                            if (a > alpha) { alpha = a; shade = 0.92f; }
                        }
                    }

                    // 3. 곡선 몸통 (Body)
                    if (py >= 80f && py <= 230f)
                    {
                        float t = (py - 80f) / 150f;
                        float bodyW = Mathf.Lerp(52f, 20f, Mathf.Pow(t, 0.7f));
                        if (dx <= bodyW)
                        {
                            float a = Mathf.Clamp01((bodyW - dx) * 1.5f);
                            if (a > alpha)
                            {
                                alpha = a;
                                shade = Mathf.Lerp(0.85f, 1.0f, 1f - (dx / bodyW));
                            }
                        }
                    }

                    // 4. 받침대 중간 턱 (Base Tier 1)
                    if (py >= 48f && py <= 84f)
                    {
                        float baseW1 = Mathf.Lerp(70f, 54f, (py - 48f) / 36f);
                        if (dx <= baseW1)
                        {
                            float a = Mathf.Clamp01((baseW1 - dx) * 1.5f);
                            if (a > alpha) { alpha = a; shade = 0.90f; }
                        }
                    }

                    // 5. 바닥 받침대 (Base Bottom)
                    if (py >= 12f && py <= 52f)
                    {
                        float baseW2 = 82f - Mathf.Abs(py - 30f) * 0.4f;
                        if (dx <= baseW2)
                        {
                            float a = Mathf.Clamp01((baseW2 - dx) * 1.5f);
                            if (a > alpha) { alpha = a; shade = 0.88f; }
                        }
                    }

                    Color col = new Color(shade, shade, shade, alpha);
                    pixels[y * width + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(PawnSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateShadowTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = dist / maxR;
                    float alpha = 0f;

                    if (norm <= 1.0f)
                    {
                        alpha = Mathf.Pow(1.0f - norm, 1.8f);
                    }

                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(ShadowSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSelectionRingTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float ringRadius = size * 0.42f;
            float ringThickness = size * 0.08f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float diff = Mathf.Abs(dist - ringRadius);
                    float alpha = 0f;

                    if (diff <= ringThickness)
                    {
                        alpha = 1.0f - (diff / ringThickness);
                        alpha = Mathf.Pow(alpha, 1.5f);
                    }

                    pixels[y * size + x] = new Color(1.0f, 0.85f, 0.15f, alpha * 0.9f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SelectionRingSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateMoveBorderTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            float outerMargin = 6f;
            float innerMargin = 16f;
            float cornerSize = 24f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float minEdgeDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    float alpha = 0f;

                    if (minEdgeDist >= outerMargin && minEdgeDist <= innerMargin)
                    {
                        float mid = (outerMargin + innerMargin) * 0.5f;
                        float d = Mathf.Abs(minEdgeDist - mid) / ((innerMargin - outerMargin) * 0.5f);
                        alpha = Mathf.Clamp01(1f - d);
                    }

                    bool isNearCornerX = (x <= outerMargin + cornerSize) || (x >= size - 1 - outerMargin - cornerSize);
                    bool isNearCornerY = (y <= outerMargin + cornerSize) || (y >= size - 1 - outerMargin - cornerSize);
                    if (isNearCornerX && isNearCornerY && minEdgeDist >= outerMargin && minEdgeDist <= innerMargin + 6f)
                    {
                        alpha = Mathf.Max(alpha, 0.95f);
                    }

                    pixels[y * size + x] = new Color(0.25f, 0.95f, 1.0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(MoveBorderSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSpriteImporter(string assetPath, Vector2 pivot, float ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);

            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static void CreatePrefab()
        {
            Sprite pawnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PawnSpritePath);
            Sprite shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
            Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SelectionRingSpritePath);
            Sprite badgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Badge_Mobility_Bubble.png");

            // 1. 루트 오브젝트: Unit
            GameObject rootObj = new GameObject("Unit_Pawn");
            Unit unitComp = rootObj.AddComponent<Unit>();

            CapsuleCollider collider = rootObj.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.75f, 0f);
            collider.radius = 0.45f;
            collider.height = 1.5f;

            // 2. 자식 1: Visual (2D 스프라이트 + 빌보드)
            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(rootObj.transform, false);
            visualObj.transform.localPosition = Vector3.zero;

            SpriteRenderer spriteRenderer = visualObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = pawnSprite;
            spriteRenderer.sortingOrder = 1;

            SpriteBillboard billboard = visualObj.AddComponent<SpriteBillboard>();
            billboard.Mode = SpriteBillboard.BillboardMode.CameraAligned;

            // 3. 자식 2: Shadow
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(rootObj.transform, false);
            shadowObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadowObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            SpriteRenderer shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.sortingOrder = 0;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.45f);

            // 4. 자식 3: SelectionRing
            GameObject ringObj = new GameObject("SelectionRing");
            ringObj.transform.SetParent(rootObj.transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ringObj.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

            SpriteRenderer ringRenderer = ringObj.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = ringSprite;
            ringRenderer.sortingOrder = 0;
            ringObj.SetActive(false);

            // 5. 자식 4: MobilityBadge (쿠키런 말풍선형 기동력 뱃지)
            GameObject badgeObj = new GameObject("MobilityBadge");
            badgeObj.transform.SetParent(rootObj.transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            badgeObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            SpriteBillboard badgeBb = badgeObj.AddComponent<SpriteBillboard>();
            badgeBb.Mode = SpriteBillboard.BillboardMode.CameraAligned;

            SpriteRenderer badgeSr = badgeObj.AddComponent<SpriteRenderer>();
            badgeSr.sprite = badgeSprite;
            badgeSr.sortingOrder = 10;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(badgeObj.transform, false);
            textObj.transform.localPosition = new Vector3(0.22f, 0.02f, -0.02f);
            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.characterSize = 0.16f;
            tm.fontSize = 32;
            tm.fontStyle = FontStyle.Bold;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.2f, 0.12f, 0.08f, 1f);
            tm.text = "3";

            badgeObj.SetActive(false);

            // 6. Unit 컴포넌트에 바인딩
            SerializedObject so = new SerializedObject(unitComp);
            so.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("_shadowRenderer").objectReferenceValue = shadowRenderer;
            so.FindProperty("_selectionIndicator").objectReferenceValue = ringObj;
            so.FindProperty("_billboard").objectReferenceValue = billboard;
            so.FindProperty("_mobilityBadgeObj").objectReferenceValue = badgeObj;
            so.FindProperty("_mobilityTextMesh").objectReferenceValue = tm;
            so.FindProperty("_mobilityBadgeRenderer").objectReferenceValue = badgeSr;
            so.ApplyModifiedProperties();

            // 7. 프리팹 에셋으로 저장
            PrefabUtility.SaveAsPrefabAsset(rootObj, PrefabPath);
            Object.DestroyImmediate(rootObj);

            Debug.Log($"[CreateUnitTexturesAndPrefab] 유닛 프리팹 저장 성공: {PrefabPath}");
        }

        public static void GenerateAllBatch()
        {
            GenerateAll();
        }
    }
}
#endif
