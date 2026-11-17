using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using _Scripts.Agent.StatSystem;
using _Scripts.Item.Enums;

namespace _Scripts.Item.Editor
{
    // 시트에서 효과로 쓸 수 있는 스탯 종류. 실제 StatData 에셋 연결은 임포터가 맡는다.
    public enum SheetStat
    {
        PhysicsPower,
        AttackSpeed,
        MoveSpeed,
        CriticalChance,
        CriticalDamage,
        MaxHealth,
    }

    public readonly struct ParsedStatEffect
    {
        public SheetStat Stat { get; }
        public float Value { get; }
        public ModifyMathType ModifyType { get; }

        public ParsedStatEffect(SheetStat stat, float value, ModifyMathType modifyType)
        {
            Stat = stat;
            Value = value;
            ModifyType = modifyType;
        }
    }

    public sealed class ItemSheetRow
    {
        public string Name;
        public string AssetName; // 에셋 파일명(영문). 비어 있으면 Name을 쓴다.
        public string IconName; // 아이콘 파일명(확장자 제외). 비어 있으면 아이콘을 건드리지 않는다.
        public string GradeText;
        public string PassiveText;
        public string ConditionalText;
        public string BuyPriceText;
        public string SellPriceText;
    }

    // CSV 텍스트를 읽어 시트 행과 상시 효과 문구로 바꾼다. 유니티 에셋은 건드리지 않는다.
    public static class ItemSheetParser
    {
        private static readonly Regex EffectRegex = new(
            @"^(?<name>[^+\-]+?)(?<sign>[+\-])(?<num>\d+(\.\d+)?)(?<unit>%p|%)?$",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, SheetStat> StatNames = new()
        {
            { "물리공격력", SheetStat.PhysicsPower },
            { "공격속도", SheetStat.AttackSpeed },
            { "이동속도", SheetStat.MoveSpeed },
            { "치명타확률", SheetStat.CriticalChance },
            { "치명타피해", SheetStat.CriticalDamage },
            { "최대HP", SheetStat.MaxHealth },
        };

        public static List<ItemSheetRow> ParseRows(string csv)
        {
            List<string[]> table = ParseCsv(csv);
            var rows = new List<ItemSheetRow>();
            if (table.Count == 0) return rows;

            string[] header = table[0];
            int name = FindColumn(header, "아이템명");
            int grade = FindColumn(header, "등급");
            int passive = FindColumn(header, "상시 효과");
            int conditional = FindColumn(header, "조건부 효과");
            int buy = FindColumn(header, "구매가");
            int sell = FindColumn(header, "판매가");
            int assetName = FindColumn(header, "에셋명", required: false);
            int icon = FindColumn(header, "아이콘", required: false);

            for (int i = 1; i < table.Count; i++)
            {
                string[] cells = table[i];
                string itemName = Cell(cells, name);
                if (string.IsNullOrEmpty(itemName)) continue;

                rows.Add(new ItemSheetRow
                {
                    Name = itemName,
                    AssetName = Cell(cells, assetName),
                    IconName = Cell(cells, icon),
                    GradeText = Cell(cells, grade),
                    PassiveText = Cell(cells, passive),
                    ConditionalText = Cell(cells, conditional),
                    BuyPriceText = Cell(cells, buy),
                    SellPriceText = Cell(cells, sell),
                });
            }

            return rows;
        }

        // "물리 공격력 +15", "공격 속도 +8% · 이동속도 +4%", "치명타 확률 +4%p" 같은 문구를 효과 목록으로 바꾼다.
        // 값은 단위로 계산 방식을 정한다: 숫자만이면 합연산, %p면 합연산(÷100), %면 곱연산(÷100).
        public static bool TryParseEffects(string passiveText, out List<ParsedStatEffect> effects, out string error)
        {
            effects = new List<ParsedStatEffect>();
            error = null;

            foreach (string token in passiveText.Split('·'))
            {
                string compact = Regex.Replace(token, @"\s+", "");
                Match match = EffectRegex.Match(compact);
                if (!match.Success)
                {
                    error = $"효과 문구를 읽을 수 없음: '{token.Trim()}'";
                    return false;
                }

                string statName = match.Groups["name"].Value;
                if (!StatNames.TryGetValue(statName, out SheetStat stat))
                {
                    error = $"연결된 스탯이 없음: '{statName}'";
                    return false;
                }

                float value = float.Parse(match.Groups["num"].Value, CultureInfo.InvariantCulture);
                if (match.Groups["sign"].Value == "-") value = -value;

                string unit = match.Groups["unit"].Value;
                ModifyMathType type = ModifyMathType.Add;
                if (unit.Length > 0) value /= 100f;
                if (unit == "%") type = ModifyMathType.Multiply;

                effects.Add(new ParsedStatEffect(stat, value, type));
            }

            return effects.Count > 0;
        }

        public static bool TryParseGrade(string text, out Grade grade)
        {
            switch (text.Trim())
            {
                case "일반": grade = Grade.Normal; return true;
                case "레어": grade = Grade.Rare; return true;
                case "유니크": grade = Grade.Unique; return true;
                case "전설": grade = Grade.Legendary; return true;
                default: grade = default; return false;
            }
        }

        // 시트에서 값이 없다는 뜻으로 쓰는 표기('-', '없음', 빈 칸)
        public static bool IsEmptyCell(string text)
            => string.IsNullOrWhiteSpace(text) || text.Trim() == "-" || text.Trim() == "없음";

        private static int FindColumn(string[] header, string prefix, bool required = true)
        {
            for (int i = 0; i < header.Length; i++)
            {
                if (header[i].Trim().StartsWith(prefix, StringComparison.Ordinal)) return i;
            }

            if (!required) return -1;
            throw new FormatException($"CSV 헤더에 '{prefix}' 열이 없습니다.");
        }

        private static string Cell(string[] cells, int index)
            => index >= 0 && index < cells.Length ? cells[index].Trim() : string.Empty;

        // 따옴표 안의 쉼표/줄바꿈을 처리하는 최소한의 CSV 파서
        private static List<string[]> ParseCsv(string text)
        {
            var table = new List<string[]>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            text = text.TrimStart('﻿');

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else inQuotes = false;
                    }
                    else cell.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"': inQuotes = true; break;
                    case ',': row.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        table.Add(row.ToArray());
                        row.Clear();
                        break;
                    default: cell.Append(c); break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                table.Add(row.ToArray());
            }

            return table;
        }
    }
}
