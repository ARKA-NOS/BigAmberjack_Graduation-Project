using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SDW.Editor
{
    // 상자/NPC 스프라이트 피벗과 프리팹별 콜라이더·UI 기준점을 맞추는 일회성 도구.
    // 스폰 포인트는 바닥 타일 바로 위 칸의 중심(바닥에서 0.5)에 놓여 있으므로,
    // 오브젝트 원점이 발(바닥)에서 0.5 위에 오도록 피벗을 잡는다.
    // 메뉴: Tools/SDW/Setup Interact Prefabs
    public static class InteractPrefabSetup
    {
        private const string ChestSheet = "Assets/SDW/Asset/Animated Chests/Chests.png";
        private const string NpcSheet = "Assets/SDW/Asset/SHADOW Series - The Free Assets/SHADOW Series - The Free Assets/SHADOW Series - The Blind Huntress/Sprite Sheet/1. Idle 48 x 48.png";

        private const float PixelsPerUnit = 16f;
        private const float OriginAboveFeet = 8f; // 0.5 유닛

        // 상자: 셀 폭 48, 몸통 중심 x=16, 바닥 = 스프라이트 하단
        private const float ChestCellWidth = 48f;
        private const float ChestBodyCenterX = 16f;

        // NPC: 셀 폭 48, 중심 x=24, 발 y=4
        private const float NpcCellWidth = 48f;
        private const float NpcCenterX = 24f;
        private const float NpcFeetY = 4f;

        private struct PrefabSetup
        {
            public string Path;
            public Vector2 ColliderSize;
            public Vector2 ColliderOffset;
            public Vector2 UiAnchor;
            public bool ResetColor;
        }

        [MenuItem("Tools/SDW/Setup Interact Prefabs")]
        public static void Setup()
        {
            SetPivots(ChestSheet, (rect) => new Vector2(
                ChestBodyCenterX - (rect.x - Mathf.Floor(rect.x / ChestCellWidth) * ChestCellWidth),
                OriginAboveFeet));

            SetPivots(NpcSheet, (rect) => new Vector2(
                NpcCenterX - (rect.x - Mathf.Floor(rect.x / NpcCellWidth) * NpcCellWidth),
                NpcFeetY - rect.y + OriginAboveFeet));

            // 스프라이트 크기: 상자 약 1.9 x 1.4, NPC 약 1.1 x 2.6 (발이 y=-0.5)
            var chest = new PrefabSetup
            {
                ColliderSize = new Vector2(2.4f, 1.8f),
                ColliderOffset = new Vector2(0f, 0.4f),
                UiAnchor = new Vector2(0f, 0.9f),
                ResetColor = true
            };
            var npc = new PrefabSetup
            {
                ColliderSize = new Vector2(1.6f, 2.6f),
                ColliderOffset = new Vector2(0f, 0.8f),
                UiAnchor = new Vector2(0f, 2.1f),
                ResetColor = false
            };

            Apply("Assets/SDW/Prefabs/Chest/Chest_Item.prefab", chest);
            Apply("Assets/SDW/Prefabs/Chest/Chest_Coin.prefab", chest);
            Apply("Assets/SDW/Prefabs/NPC/Npc_EccentricDoctor.prefab", npc);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[InteractPrefabSetup] 완료");
        }

        private static void SetPivots(string path, System.Func<Rect, Vector2> pivotPixels)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] rects = provider.GetSpriteRects();
            foreach (SpriteRect spriteRect in rects)
            {
                Vector2 px = pivotPixels(spriteRect.rect);
                spriteRect.alignment = SpriteAlignment.Custom;
                spriteRect.pivot = new Vector2(px.x / spriteRect.rect.width, px.y / spriteRect.rect.height);
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void Apply(string prefabPath, PrefabSetup setup)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var box = root.GetComponent<BoxCollider2D>();
                box.size = setup.ColliderSize;
                box.offset = setup.ColliderOffset;

                if (setup.ResetColor)
                    root.GetComponent<SpriteRenderer>().color = Color.white;

                var interactable = root.GetComponent<Lrw.Script.Agent.Players.InteractSystem.AbstractInteractObject>();
                var so = new SerializedObject(interactable);
                so.FindProperty("uiAnchorOffset").vector2Value = setup.UiAnchor;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
