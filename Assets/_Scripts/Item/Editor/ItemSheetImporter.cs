using System;
using System.Collections.Generic;
using System.Globalization;
using _Scripts.Agent.StatSystem;
using _Scripts.Item.Effects;
using _Scripts.Item.Enums;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Item.Editor
{
    public sealed class ItemSheetImportResult
    {
        public readonly List<string> Created = new();
        public readonly List<string> Updated = new();
        public readonly List<string> Skipped = new();
        public readonly List<string> Warnings = new();
    }

    // 시트 행을 ItemSO / StatItemEffect 에셋으로 만든다.
    // 상시 효과만 있는 행만 가져오고, 이미 있는 아이템은 이름·등급·효과·가격만 갱신한다.
    // (ID, 설명, 아이콘, 태그, 획득 경로는 건드리지 않는다.)
    public static class ItemSheetImporter
    {
        private const string ItemFolder = "Assets/_Scripts/Item/ItemData";
        private const string EffectFolder = "Assets/_Scripts/Item/Effects/ItemEffectData";
        private const string StatFolder = "Assets/GameModules/Stats";
        private const string IconFolder = "Assets/_Graphics/Assets/UI/Honeti/PixelArtGUI/Textures/Icons/32";

        private static readonly Dictionary<SheetStat, string> StatAssetPaths = new()
        {
            { SheetStat.PhysicsPower, StatFolder + "/Physics Power.asset" },
            { SheetStat.AttackSpeed, StatFolder + "/Attack Speed.asset" },
            { SheetStat.MoveSpeed, StatFolder + "/MoveSpeed.asset" },
            { SheetStat.CriticalChance, StatFolder + "/Critical Chance.asset" },
            { SheetStat.CriticalDamage, StatFolder + "/Critical Damage.asset" },
            { SheetStat.MaxHealth, StatFolder + "/Health/MaxHealth.asset" },
        };

        private static readonly Dictionary<SheetStat, string> EffectNames = new()
        {
            { SheetStat.PhysicsPower, "Physics Power" },
            { SheetStat.AttackSpeed, "Attack Speed" },
            { SheetStat.MoveSpeed, "Move Speed" },
            { SheetStat.CriticalChance, "Critical Chance" },
            { SheetStat.CriticalDamage, "Critical Damage" },
            { SheetStat.MaxHealth, "Max Health" },
        };

        public static ItemSheetImportResult Import(string csvText)
        {
            var result = new ItemSheetImportResult();

            foreach (ItemSheetRow row in ItemSheetParser.ParseRows(csvText))
            {
                if (!TryBuildItem(row, out Grade grade, out int buy, out int sell,
                        out List<ParsedStatEffect> effects, out string skipReason))
                {
                    result.Skipped.Add($"{row.Name} - {skipReason}");
                    continue;
                }

                var effectAssets = new List<StatItemEffect>();
                foreach (ParsedStatEffect effect in effects)
                {
                    StatItemEffect asset = GetOrCreateEffect(effect);
                    if (asset == null)
                    {
                        skipReason = $"스탯 에셋을 찾을 수 없음: {StatAssetPaths[effect.Stat]}";
                        break;
                    }

                    effectAssets.Add(asset);
                }

                if (effectAssets.Count != effects.Count)
                {
                    result.Skipped.Add($"{row.Name} - {skipReason}");
                    continue;
                }

                string assetName = string.IsNullOrEmpty(row.AssetName) ? row.Name : row.AssetName;
                Sprite icon = LoadIcon(row, result);
                bool created = ApplyItem(assetName, row.Name, grade, buy, sell, effectAssets, icon);
                (created ? result.Created : result.Updated).Add(row.Name);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static bool TryBuildItem(ItemSheetRow row, out Grade grade, out int buy, out int sell,
            out List<ParsedStatEffect> effects, out string skipReason)
        {
            grade = default;
            buy = 0;
            sell = 0;
            effects = null;
            skipReason = null;

            if (ItemSheetParser.IsEmptyCell(row.PassiveText))
            {
                skipReason = "상시 효과 없음";
                return false;
            }

            if (!ItemSheetParser.IsEmptyCell(row.ConditionalText))
            {
                skipReason = "조건부 효과 있음";
                return false;
            }

            if (!ItemSheetParser.TryParseEffects(row.PassiveText, out effects, out string error))
            {
                skipReason = error;
                return false;
            }

            if (!ItemSheetParser.TryParseGrade(row.GradeText, out grade))
            {
                skipReason = $"등급을 읽을 수 없음: '{row.GradeText}'";
                return false;
            }

            if (!int.TryParse(row.BuyPriceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out buy) ||
                !int.TryParse(row.SellPriceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sell))
            {
                skipReason = $"가격을 읽을 수 없음: '{row.BuyPriceText}' / '{row.SellPriceText}'";
                return false;
            }

            return true;
        }

        // 스탯/값/계산 방식이 같으면 에셋 하나를 여러 아이템이 공유한다.
        private static StatItemEffect GetOrCreateEffect(ParsedStatEffect effect)
        {
            var statData = AssetDatabase.LoadAssetAtPath<StatData>(StatAssetPaths[effect.Stat]);
            if (statData == null) return null;

            string path = $"{EffectFolder}/{EffectAssetName(effect)}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<StatItemEffect>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StatItemEffect>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("targetStat").objectReferenceValue = statData;
            so.FindProperty("priority").intValue = 0;
            so.FindProperty("value").floatValue = effect.Value;
            so.FindProperty("modifyType").enumValueIndex = (int)effect.ModifyType;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // 예: "Physics Power +15", "Attack Speed +7%", "Critical Chance +4%p"
        private static string EffectAssetName(ParsedStatEffect effect)
        {
            bool multiply = effect.ModifyType == ModifyMathType.Multiply;
            bool percentPoint = !multiply &&
                                (effect.Stat == SheetStat.CriticalChance || effect.Stat == SheetStat.CriticalDamage);

            float shown = multiply || percentPoint ? effect.Value * 100f : effect.Value;
            string sign = shown >= 0 ? "+" : "-";
            string number = Math.Abs(shown).ToString("0.##", CultureInfo.InvariantCulture);
            string unit = multiply ? "%" : percentPoint ? "%p" : string.Empty;

            return $"{EffectNames[effect.Stat]} {sign}{number}{unit}";
        }

        // 새로 만들었으면 true, 기존 에셋을 갱신했으면 false
        // 시트에 아이콘 이름이 없거나 파일을 못 찾으면 null(아이콘은 건드리지 않는다)
        private static Sprite LoadIcon(ItemSheetRow row, ItemSheetImportResult result)
        {
            if (string.IsNullOrEmpty(row.IconName)) return null;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{row.IconName}.png");
            if (sprite == null) result.Warnings.Add($"{row.Name} - 아이콘을 찾을 수 없음: {row.IconName}");
            return sprite;
        }

        // assetName은 파일명(영문), displayName은 인스펙터의 아이템 이름(한글)
        private static bool ApplyItem(string assetName, string displayName, Grade grade, int buy, int sell,
            List<StatItemEffect> effects, Sprite icon)
        {
            string path = $"{ItemFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<ItemSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("<ItemName>k__BackingField").stringValue = displayName;
            so.FindProperty("<Grade>k__BackingField").enumValueIndex = (int)grade;
            so.FindProperty("<BuyPrice>k__BackingField").intValue = buy;
            so.FindProperty("<SellPrice>k__BackingField").intValue = sell;
            if (icon != null) so.FindProperty("<Icon>k__BackingField").objectReferenceValue = icon;

            SerializedProperty list = so.FindProperty("<Effects>k__BackingField");
            list.arraySize = effects.Count;
            for (int i = 0; i < effects.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return created;
        }
    }
}
