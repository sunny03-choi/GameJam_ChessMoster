#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChessMonsters.UI.Editor
{
    public static class CreateActionMenuTextures
    {
        private const string UiDir = "Assets/Textures/UI";
        private const string UnitsDir = "Assets/Textures/Units";

        [MenuItem("Chess Monsters/Create Action Menu Textures")]
        public static void GenerateAll()
        {
            EnsureDirectories();

            CreateAttackButtonTexture();
            CreateMoveButtonTexture();
            CreateSkillButtonTexture();
            CreateMobilityBadgeTexture();
            CreateTargetReticleTexture();
            CreateBattleStartBannerTexture();
            CreateEnemyPawnSpriteTexture();

            AssetDatabase.Refresh();

            ConfigureSprite(UiDir + "/Btn_Cookie_Attack.png", new Vector2(0.5f, 0.5f));
            ConfigureSprite(UiDir + "/Btn_Cookie_Move.png", new Vector2(0.5f, 0.5f));
            ConfigureSprite(UiDir + "/Btn_Cookie_Skill.png", new Vector2(0.5f, 0.5f));
            ConfigureSprite(UiDir + "/Badge_Mobility_Bubble.png", new Vector2(0.5f, 0.5f));
            ConfigureSprite(UiDir + "/Sprite_Target_Reticle.png", new Vector2(0.5f, 0.5f));
            ConfigureSprite(UiDir + "/Banner_Battle_Start.png", new Vector2(0.5f, 0.5f));
            ConfigureSprite(UnitsDir + "/Sprite_Unit_Pawn_Red.png", new Vector2(0.5f, 0.0f), 256f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CreateActionMenuTextures] 쿠키런 킹덤 스타일 3종 선택지 버튼, 기동력 뱃지, 적군 스프라이트 생성 완료!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
                AssetDatabase.CreateFolder("Assets", "Textures");
            if (!AssetDatabase.IsValidFolder(UiDir))
                AssetDatabase.CreateFolder("Assets/Textures", "UI");
            if (!AssetDatabase.IsValidFolder(UnitsDir))
                AssetDatabase.CreateFolder("Assets/Textures", "Units");
        }

        /// <summary>
        /// 쿠키런 킹덤 스타일의 도톰하고 귀여운 둥근 알약형 버튼 (수학적으로 매끄러운 캡슐 SDF)
        /// </summary>
        private static Texture2D CreateCookiePillButton(int width, int height, Color primaryColor, Color secondaryColor, Color darkShadowColor, System.Action<Texture2D, int, int> drawIcon)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[width * height];

            float radius = (height * 0.5f) - 3f;
            float leftCenterX = radius + 3f;
            float rightCenterX = width - radius - 3f;
            float centerY = height * 0.5f;

            float borderThickness = 4.5f;
            float innerBorderThickness = 2.5f;
            Color outlineColor = new Color(0.18f, 0.12f, 0.10f, 1f); // 짙은 초콜릿 아웃라인
            Color goldRimColor = new Color(1.0f, 0.92f, 0.65f, 1f); // 은은한 골드 림

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float d;
                    if (px < leftCenterX)
                    {
                        d = Vector2.Distance(new Vector2(px, py), new Vector2(leftCenterX, centerY));
                    }
                    else if (px > rightCenterX)
                    {
                        d = Vector2.Distance(new Vector2(px, py), new Vector2(rightCenterX, centerY));
                    }
                    else
                    {
                        d = Mathf.Abs(py - centerY);
                    }

                    float distToEdge = radius - d;

                    if (distToEdge < -0.5f)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    // 외곽 앤티앨리어싱
                    float edgeAlpha = Mathf.Clamp01(distToEdge + 0.5f);

                    // 1. 가장 바깥쪽 짙은 초콜릿 외곽선
                    if (distToEdge <= borderThickness)
                    {
                        pixels[y * width + x] = new Color(outlineColor.r, outlineColor.g, outlineColor.b, edgeAlpha);
                        continue;
                    }

                    // 2. 안쪽 샴페인 골드 테두리
                    if (distToEdge <= borderThickness + innerBorderThickness)
                    {
                        pixels[y * width + x] = goldRimColor;
                        continue;
                    }

                    // 3. 내부 그라데이션 및 3D 입체 하이라이트
                    float innerY = (py - (borderThickness + innerBorderThickness)) / (height - 2f * (borderThickness + innerBorderThickness));
                    Color bodyColor;

                    if (innerY < 0.28f)
                    {
                        bodyColor = Color.Lerp(darkShadowColor, primaryColor, innerY / 0.28f);
                    }
                    else if (innerY > 0.65f)
                    {
                        bodyColor = Color.Lerp(primaryColor, secondaryColor, (innerY - 0.65f) / 0.35f);
                    }
                    else
                    {
                        bodyColor = primaryColor;
                    }

                    // 상단 은은한 젤리 광택 (Glossy Highlight)
                    if (innerY > 0.60f && (px > leftCenterX * 0.8f && px < width - leftCenterX * 0.8f))
                    {
                        float gloss = Mathf.Pow((innerY - 0.60f) / 0.40f, 2f) * 0.35f;
                        bodyColor = Color.Lerp(bodyColor, Color.white, gloss);
                    }

                    pixels[y * width + x] = bodyColor;
                }
            }

            tex.SetPixels(pixels);

            // 아이콘 오버레이 그리기
            drawIcon?.Invoke(tex, width, height);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 1. 공격 버튼 (쿠키런 레드/크림슨 베이스 + 쌍검 아이콘)
        /// </summary>
        private static void CreateAttackButtonTexture()
        {
            int w = 260;
            int h = 88;
            Color primary = new Color(0.88f, 0.22f, 0.22f); // 비비드 레드
            Color secondary = new Color(1.0f, 0.45f, 0.35f); // 밝은 코랄 하이라이트
            Color shadow = new Color(0.48f, 0.08f, 0.08f); // 딥 크림슨 그림자

            Texture2D tex = CreateCookiePillButton(w, h, primary, secondary, shadow, (t, width, height) =>
            {
                Vector2 iconCenter = new Vector2(44f, 44f);
                float radius = 25f;

                DrawFilledCircle(t, iconCenter, radius, new Color(1f, 1f, 1f, 0.98f), new Color(0.3f, 0.1f, 0.1f, 1f), 3f);
                DrawCrossedSwords(t, (int)iconCenter.x, (int)iconCenter.y, 14);
            });

            File.WriteAllBytes(UiDir + "/Btn_Cookie_Attack.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 2. 이동 버튼 (쿠키런 에메랄드 그린/민트 베이스 + 날개 달린 부츠 아이콘)
        /// </summary>
        private static void CreateMoveButtonTexture()
        {
            int w = 260;
            int h = 88;
            Color primary = new Color(0.18f, 0.76f, 0.42f); // 비비드 에메랄드
            Color secondary = new Color(0.45f, 0.92f, 0.58f); // 밝은 민트 그린
            Color shadow = new Color(0.08f, 0.40f, 0.20f); // 딥 포레스트 그린

            Texture2D tex = CreateCookiePillButton(w, h, primary, secondary, shadow, (t, width, height) =>
            {
                Vector2 iconCenter = new Vector2(44f, 44f);
                float radius = 25f;

                DrawFilledCircle(t, iconCenter, radius, new Color(1f, 1f, 1f, 0.98f), new Color(0.08f, 0.35f, 0.15f, 1f), 3f);
                DrawWingedBoot(t, (int)iconCenter.x, (int)iconCenter.y, 14);
            });

            File.WriteAllBytes(UiDir + "/Btn_Cookie_Move.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 3. 효과 발동 버튼 (쿠키런 로열 퍼플/골드 베이스 + 마법 별 아이콘)
        /// </summary>
        private static void CreateSkillButtonTexture()
        {
            int w = 260;
            int h = 88;
            Color primary = new Color(0.58f, 0.28f, 0.88f); // 비비드 퍼플
            Color secondary = new Color(0.78f, 0.52f, 1.0f); // 라벤더 하이라이트
            Color shadow = new Color(0.32f, 0.10f, 0.52f); // 딥 퍼플 그림자

            Texture2D tex = CreateCookiePillButton(w, h, primary, secondary, shadow, (t, width, height) =>
            {
                Vector2 iconCenter = new Vector2(44f, 44f);
                float radius = 25f;

                DrawFilledCircle(t, iconCenter, radius, new Color(1f, 1f, 1f, 0.98f), new Color(0.25f, 0.08f, 0.4f, 1f), 3f);
                DrawMagicStar(t, (int)iconCenter.x, (int)iconCenter.y, 14);
            });

            File.WriteAllBytes(UiDir + "/Btn_Cookie_Skill.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 유닛 머리 위에 동동 뜨는 쿠키런 말풍선형 기동력 뱃지 배경
        /// </summary>
        private static void CreateMobilityBadgeTexture()
        {
            int w = 240;
            int h = 76;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[w * h];

            float radius = (h * 0.5f) - 3f;
            float leftCenterX = radius + 3f;
            float rightCenterX = w - radius - 3f;
            float centerY = h * 0.5f;

            Color outline = new Color(0.20f, 0.14f, 0.10f, 1.0f);
            Color goldRim = new Color(1.0f, 0.88f, 0.35f, 1.0f);
            Color fillTop = new Color(1.0f, 1.0f, 0.96f, 0.96f);
            Color fillBottom = new Color(1.0f, 0.92f, 0.78f, 0.96f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float d;
                    if (px < leftCenterX)
                        d = Vector2.Distance(new Vector2(px, py), new Vector2(leftCenterX, centerY));
                    else if (px > rightCenterX)
                        d = Vector2.Distance(new Vector2(px, py), new Vector2(rightCenterX, centerY));
                    else
                        d = Mathf.Abs(py - centerY);

                    float distToEdge = radius - d;

                    if (distToEdge < -0.5f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float a = Mathf.Clamp01(distToEdge + 0.5f);

                    if (distToEdge <= 4f)
                    {
                        pixels[y * w + x] = new Color(outline.r, outline.g, outline.b, a);
                        continue;
                    }

                    if (distToEdge <= 6.5f)
                    {
                        pixels[y * w + x] = goldRim;
                        continue;
                    }

                    float t = (float)y / h;
                    pixels[y * w + x] = Color.Lerp(fillBottom, fillTop, t);
                }
            }

            tex.SetPixels(pixels);

            // 왼쪽에 신발/발자국 미니 아이콘
            DrawWingedBoot(tex, 36, 38, 14);

            tex.Apply();
            File.WriteAllBytes(UiDir + "/Badge_Mobility_Bubble.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 공격 범위 내 적 타겟팅 조준선(Reticle)
        /// </summary>
        private static void CreateTargetReticleTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.38f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float diff = Mathf.Abs(dist - radius);
                    float alpha = 0f;

                    if (diff <= 5f)
                    {
                        alpha = 1f - (diff / 5f);
                    }

                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    if ((dx <= 2f && dist > 14f && dist < radius + 12f) ||
                        (dy <= 2f && dist > 14f && dist < radius + 12f))
                    {
                        alpha = Mathf.Max(alpha, 0.95f);
                    }

                    if (dist <= 4f)
                    {
                        alpha = Mathf.Max(alpha, 1.0f);
                    }

                    pixels[y * size + x] = new Color(1.0f, 0.18f, 0.18f, alpha * 0.95f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(UiDir + "/Sprite_Target_Reticle.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// "전투 진입 (BATTLE START!)" 컷인 배너
        /// </summary>
        private static void CreateBattleStartBannerTexture()
        {
            int w = 512;
            int h = 120;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color darkBorder = new Color(0.12f, 0.08f, 0.06f, 0.98f);
            Color fireGold = new Color(1.0f, 0.82f, 0.20f, 1f);
            Color fireCrimson = new Color(0.85f, 0.15f, 0.15f, 0.95f);
            Color fireOrange = new Color(0.98f, 0.45f, 0.12f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float normX = (float)x / w;
                    float normY = (float)y / h;

                    if (normX < 0.06f)
                    {
                        float d = Mathf.Abs(normY - 0.5f) * 2f;
                        if (normX < 0.06f * (1f - d)) { pixels[y * w + x] = Color.clear; continue; }
                    }
                    else if (normX > 0.94f)
                    {
                        float d = Mathf.Abs(normY - 0.5f) * 2f;
                        if ((1f - normX) < 0.06f * (1f - d)) { pixels[y * w + x] = Color.clear; continue; }
                    }

                    if (y <= 4 || y >= h - 5)
                    {
                        pixels[y * w + x] = darkBorder;
                        continue;
                    }
                    if (y <= 8 || y >= h - 9)
                    {
                        pixels[y * w + x] = fireGold;
                        continue;
                    }

                    float distCenter = Mathf.Abs(normX - 0.5f);
                    Color baseCol = Color.Lerp(fireOrange, fireCrimson, distCenter * 1.5f);
                    pixels[y * w + x] = baseCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(UiDir + "/Banner_Battle_Start.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 적군 유닛(Red Monster Pawn) 2D 스프라이트
        /// </summary>
        private static void CreateEnemyPawnSpriteTexture()
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
                    Color tint = new Color(0.22f, 0.18f, 0.25f, 1f);

                    // 1. 머리 원형 구체 (Head)
                    float headDist = Vector2.Distance(new Vector2(px, py), new Vector2(128f, 285f));
                    if (headDist <= 46f)
                    {
                        alpha = Mathf.Clamp01((46f - headDist) * 1.5f);
                        tint = Color.Lerp(new Color(0.38f, 0.15f, 0.22f), new Color(0.16f, 0.12f, 0.18f), headDist / 46f);

                        // 붉은 악마 눈
                        float leftEye = Vector2.Distance(new Vector2(px, py), new Vector2(112f, 285f));
                        float rightEye = Vector2.Distance(new Vector2(px, py), new Vector2(144f, 285f));
                        if (leftEye <= 6f || rightEye <= 6f)
                        {
                            tint = new Color(1.0f, 0.15f, 0.15f, 1f);
                        }
                    }

                    // 2. 작은 뿔
                    float leftHorn = Vector2.Distance(new Vector2(px, py), new Vector2(98f, 325f));
                    float rightHorn = Vector2.Distance(new Vector2(px, py), new Vector2(158f, 325f));
                    if (leftHorn <= 12f || rightHorn <= 12f)
                    {
                        alpha = Mathf.Max(alpha, 0.95f);
                        tint = new Color(0.85f, 0.2f, 0.2f, 1f);
                    }

                    // 3. 목 링
                    if (py >= 226f && py <= 244f)
                    {
                        float collarW = 34f;
                        if (dx <= collarW)
                        {
                            float a = Mathf.Clamp01((collarW - dx) * 1.5f);
                            if (a > alpha) { alpha = a; tint = new Color(0.85f, 0.70f, 0.20f); }
                        }
                    }

                    // 4. 곡선 몸통
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
                                tint = Color.Lerp(new Color(0.20f, 0.14f, 0.18f), new Color(0.35f, 0.15f, 0.20f), 1f - (dx / bodyW));

                                float coreDist = Vector2.Distance(new Vector2(px, py), new Vector2(128f, 160f));
                                if (coreDist <= 12f)
                                {
                                    tint = Color.Lerp(new Color(1.0f, 0.2f, 0.2f), tint, coreDist / 12f);
                                }
                            }
                        }
                    }

                    // 5. 받침대
                    if (py >= 12f && py <= 84f)
                    {
                        float baseW = (py > 50f) ? Mathf.Lerp(70f, 54f, (py - 48f) / 36f) : (82f - Mathf.Abs(py - 30f) * 0.4f);
                        if (dx <= baseW)
                        {
                            float a = Mathf.Clamp01((baseW - dx) * 1.5f);
                            if (a > alpha)
                            {
                                alpha = a;
                                tint = new Color(0.18f, 0.14f, 0.18f);
                            }
                        }
                    }

                    pixels[y * width + x] = new Color(tint.r, tint.g, tint.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(UnitsDir + "/Sprite_Unit_Pawn_Red.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ================= 도형 그리기 헬퍼 =================
        private static void DrawFilledCircle(Texture2D tex, Vector2 center, float radius, Color fillColor, Color outlineColor, float outlineWidth)
        {
            int minX = Mathf.Max(0, (int)(center.x - radius - 2));
            int maxX = Mathf.Min(tex.width - 1, (int)(center.x + radius + 2));
            int minY = Mathf.Max(0, (int)(center.y - radius - 2));
            int maxY = Mathf.Min(tex.height - 1, (int)(center.y + radius + 2));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius) continue;

                    if (dist >= radius - outlineWidth)
                    {
                        tex.SetPixel(x, y, outlineColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                }
            }
        }

        private static void DrawCrossedSwords(Texture2D tex, int cx, int cy, int size)
        {
            Color blade = new Color(0.92f, 0.95f, 1.0f, 1.0f);
            Color hilt = new Color(0.95f, 0.75f, 0.15f, 1.0f);
            Color dark = new Color(0.2f, 0.15f, 0.1f, 1.0f);

            for (int i = -size; i <= size; i++)
            {
                int x = cx + i;
                int y = cy + i;
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                {
                    tex.SetPixel(x, y, (i < -size + 5) ? hilt : blade);
                    if (x + 1 < tex.width) tex.SetPixel(x + 1, y, dark);
                }
            }

            for (int i = -size; i <= size; i++)
            {
                int x = cx + i;
                int y = cy - i;
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                {
                    tex.SetPixel(x, y, (i < -size + 5) ? hilt : blade);
                    if (x + 1 < tex.width) tex.SetPixel(x + 1, y, dark);
                }
            }
        }

        private static void DrawWingedBoot(Texture2D tex, int cx, int cy, int size)
        {
            Color bootColor = new Color(0.35f, 0.78f, 0.95f, 1.0f);
            Color wingColor = new Color(1.0f, 0.90f, 0.25f, 1.0f);

            for (int dy = -size; dy <= size; dy++)
            {
                for (int dx = -size; dx <= size; dx++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px < 0 || px >= tex.width || py < 0 || py >= tex.height) continue;

                    bool isSole = (dy <= -size + 4 && dx >= -size + 2 && dx <= size - 1);
                    bool isShaft = (dx <= -2 && dx >= -size + 4 && dy >= -size + 4 && dy <= size - 2);

                    if (isSole || isShaft)
                    {
                        tex.SetPixel(px, py, bootColor);
                    }

                    if (dx <= -size + 4 && dy >= -2 && dy <= size)
                    {
                        tex.SetPixel(px, py, wingColor);
                    }
                }
            }
        }

        private static void DrawMagicStar(Texture2D tex, int cx, int cy, int size)
        {
            Color starCol = new Color(1.0f, 0.92f, 0.25f, 1.0f);
            Color glowCol = new Color(1.0f, 0.65f, 0.15f, 0.9f);

            for (int dy = -size; dy <= size; dy++)
            {
                for (int dx = -size; dx <= size; dx++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px < 0 || px >= tex.width || py < 0 || py >= tex.height) continue;

                    float d = Mathf.Sqrt(Mathf.Abs(dx)) + Mathf.Sqrt(Mathf.Abs(dy));
                    if (d <= Mathf.Sqrt(size))
                    {
                        tex.SetPixel(px, py, starCol);
                    }
                    else if (d <= Mathf.Sqrt(size) + 0.6f)
                    {
                        tex.SetPixel(px, py, glowCol);
                    }
                }
            }
        }

        private static void ConfigureSprite(string path, Vector2 pivot, float ppu = 128f)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
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
    }
}
#endif
