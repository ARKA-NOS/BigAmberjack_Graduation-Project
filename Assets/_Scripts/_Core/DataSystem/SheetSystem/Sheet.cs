using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace _Scripts._Core.DataSystem.SheetSystem
{
    [Serializable]
    public class Sheet : IEnumerable<RowData>
    {
        private const char Separator = ',';
        private const char Quote = '"';
        private const char Bom = '﻿';

        private readonly List<RowData> _sheetData = new();
        
        public RowData this[int index] => _sheetData[index];
        
        public Sheet(string csvData)
        {
            if (string.IsNullOrEmpty(csvData))
                return;

            List<List<string>> rows = ParseCsv(csvData);

            int headerIndex = rows.FindIndex(fields => !IsEmptyRow(fields));
            if (headerIndex < 0)
                return;

            List<string> headers = new();
            foreach (string header in rows[headerIndex])
                headers.Add(header.Trim());

            for (int i = headerIndex + 1; i < rows.Count; i++)
            {
                List<string> fields = rows[i];
                if (IsEmptyRow(fields))
                    continue;

                _sheetData.Add(CreateRow(fields, headers));
            }
        }
        
        private RowData CreateRow(List<string> fields, List<string> headers)
        {
            RowData row = new RowData();
            
            // 헤더보다 많은 필드는 버리고, 모자란 필드는 RowData가 빈 문자열을 돌려준다.
            int columnCount = Math.Min(headers.Count, fields.Count);
            for (int column = 0; column < columnCount; column++)
            {
                string header = headers[column];
                if (string.IsNullOrEmpty(header))
                    continue; // 이름 없는 열은 무시한다.

                row[header] = fields[column];
            }
            
            return row;
        }
        
        private static bool IsEmptyRow(List<string> fields)
        {
            foreach (string field in fields)
            {
                if (!string.IsNullOrWhiteSpace(field))
                    return false;
            }

            return true;
        }
        
        private static List<List<string>> ParseCsv(string csv)
        {
            List<List<string>> rows = new();
            List<string> fields = new();
            StringBuilder field = new();
            bool inQuotes = false;

            int start = csv[0] == Bom ? 1 : 0;

            for (int i = start; i < csv.Length; i++)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c != Quote)
                    {
                        field.Append(c);
                    }
                    else if (i + 1 < csv.Length && csv[i + 1] == Quote)
                    {
                        field.Append(Quote); // "" → "
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }

                    continue;
                }

                switch (c)
                {
                    case Quote when field.Length == 0:
                        inQuotes = true;
                        break;
                    case Separator:
                        EndField(fields, field);
                        break;
                    case '\r':
                        if (i + 1 < csv.Length && csv[i + 1] == '\n')
                            i++; // CRLF는 줄바꿈 하나로 본다.
                        EndRow(ref fields, field, rows);
                        break;
                    case '\n':
                        EndRow(ref fields, field, rows);
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            // 마지막 줄 끝에 줄바꿈이 없으면 남은 행을 마저 넣는다.
            if (field.Length > 0 || fields.Count > 0)
                EndRow(ref fields, field, rows);

            return rows;
        }
        
        private static void EndRow(ref List<string> fields, StringBuilder field, List<List<string>> rows)
        {
            EndField(fields, field);
            rows.Add(fields);
            fields = new List<string>();
        }
        
        private static void EndField(List<string> fields, StringBuilder field)
        {
            fields.Add(field.ToString());
            field.Clear();
        }
        
        public IEnumerator<RowData> GetEnumerator() => _sheetData.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
    }
}
