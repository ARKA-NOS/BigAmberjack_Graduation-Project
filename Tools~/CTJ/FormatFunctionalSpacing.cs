using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityPipeline.Microsoft.CodeAnalysis;
using UnityPipeline.Microsoft.CodeAnalysis.CSharp;
using UnityPipeline.Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityPipeline.Microsoft.CodeAnalysis.Text;

public static class FormatFunctionalSpacing
{
    private static readonly string[] Roots = {
        "Assets/_Scripts/Agent/Enemies", "Assets/_Scripts/UI/Enemy",
        "Assets/_Scripts/UI/HitFeedback", "Assets/_Scripts/Test/Enemy"
    };

    // 사용자가 별도로 승인했고 우리가 작성한 오디오 코드만 포함합니다.
    private static readonly string[] AudioFiles = {
        "SoundManager.cs", "SoundEmitter.cs", "SoundCueSO.cs", "ISoundManager.cs", "NullSoundManager.cs"
    };

    public static object Preview() => Process(false);

    public static object Run() => Process(true);

    private static object Process(bool apply)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        var paths = Roots.SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            .Concat(AudioFiles.Select(name => "Assets/_Scripts/Audio/" + name)).OrderBy(path => path).ToArray();
        var replacements = new List<(string path, string content, bool bom, int lines, string tokenHash)>();
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
            .WithPreprocessorSymbols("UNITY_EDITOR", "UNITY_6000_0_OR_NEWER", "UNITY_6000_3_OR_NEWER");

        foreach (string path in paths)
        {
            string original = File.ReadAllText(path);
            var tree = CSharpSyntaxTree.ParseText(original, options);
            if (tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                throw new Exception("Cannot format invalid syntax: " + path);

            SyntaxNode root = tree.GetRoot();
            SourceText source = tree.GetText();
            var blankLines = new SortedSet<int>();

            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                for (int i = 1; i < type.Members.Count; i++)
                {
                    var previous = type.Members[i - 1];
                    var current = type.Members[i];
                    string category = Category(current);
                    if (category == "method" || category == "type" || Category(previous) != category || HasHeader(current) ||
                        (current is PropertyDeclarationSyntax p && p.AccessorList != null &&
                         p.AccessorList.Accessors.Any(a => a.Body != null)))
                        AddBefore(current, previous);
                }
            }

            foreach (var block in root.DescendantNodes().OfType<BlockSyntax>()) SeparateStatements(block.Statements);
            foreach (var section in root.DescendantNodes().OfType<SwitchSectionSyntax>()) SeparateStatements(section.Statements);
            foreach (var statement in root.DescendantNodes().OfType<SwitchStatementSyntax>())
                for (int i = 1; i < statement.Sections.Count; i++) AddBefore(statement.Sections[i], statement.Sections[i - 1]);

            if (blankLines.Count == 0) continue;
            string newline = original.Contains("\r\n") ? "\r\n" : "\n";
            var builder = new StringBuilder(original);
            foreach (int line in blankLines.Reverse()) builder.Insert(source.Lines[line].Start, newline);
            string formatted = builder.ToString();
            var afterTree = CSharpSyntaxTree.ParseText(formatted, options);
            string beforeTokens = Tokens(root), afterTokens = Tokens(afterTree.GetRoot());
            if (beforeTokens != afterTokens ||
                !original.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line))
                    .SequenceEqual(formatted.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line))))
                throw new Exception("Formatting changed code or comments: " + path);

            byte[] bytes = File.ReadAllBytes(path);
            bool bom = bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191;
            using (var sha = SHA256.Create())
                replacements.Add((path.Replace('\\', '/'), formatted, bom, blankLines.Count,
                    BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(beforeTokens))).Replace("-", "")));

            void AddBefore(SyntaxNode current, SyntaxNode previous)
            {
                int line = source.Lines.GetLineFromPosition(current.SpanStart).LineNumber;
                int previousEnd = source.Lines.GetLineFromPosition(previous.Span.End - 1).LineNumber;
                if (line <= previousEnd || line == 0) return;

                // 주석과 해당 코드가 떨어지지 않도록 주석 묶음 앞에 빈 줄을 넣습니다.
                while (line > previousEnd + 1 && IsComment(source.Lines[line - 1].ToString().TrimStart())) line--;
                if (line > previousEnd && !string.IsNullOrWhiteSpace(source.Lines[line - 1].ToString())) blankLines.Add(line);
            }

            void SeparateStatements(SyntaxList<StatementSyntax> statements)
            {
                for (int i = 1; i < statements.Count; i++)
                {
                    var previous = statements[i - 1];
                    var current = statements[i];
                    if (IsGuard(previous) && IsGuard(current)) continue;
                    if (CompactIf(previous) && CompactIf(current) && !IsGuard(previous) && !IsGuard(current)) continue;
                    if (previous is LocalDeclarationStatementSyntax && IsGuard(current)) continue;
                    if (IsControl(previous) || IsControl(current) || current is ReturnStatementSyntax ||
                        (previous is LocalDeclarationStatementSyntax && current is ExpressionStatementSyntax))
                        AddBefore(current, previous);
                }
            }

            bool CompactIf(StatementSyntax statement) => statement is IfStatementSyntax condition && condition.Else == null &&
                !(condition.Statement is BlockSyntax) &&
                source.Lines.GetLineFromPosition(statement.SpanStart).LineNumber ==
                source.Lines.GetLineFromPosition(statement.Span.End - 1).LineNumber;
        }

        if (apply)
        {
            foreach (var item in replacements) File.WriteAllText(item.path, item.content, new UTF8Encoding(item.bom));
            var report = new StringBuilder("# 기능별 빈 줄 정리 검증\n\n작업일: 2026-11-24\n\n");
            report.AppendLine($"검토 {paths.Length}개, 변경 {replacements.Count}개, 빈 줄 추가 {replacements.Sum(item => item.lines)}개.\n");
            report.AppendLine("모든 변경 파일에서 C# 토큰과 비어 있지 않은 원문 줄이 전후 동일하다. 아래 해시는 공백·주석을 제외한 코드 토큰을 기준으로 하며 전후 같은 값이다. 기존 들여쓰기, 줄 내용, 문자열, 주석과 파일 BOM·줄바꿈 형식은 유지했다.\n");
            report.AppendLine("| 파일 | 추가 빈 줄 | 전후 동일 토큰 SHA256 |\n| --- | ---: | --- |");
            foreach (var item in replacements) report.AppendLine($"| {item.path} | {item.lines} | {item.tokenHash} |");
            File.WriteAllText("Tools~/CTJ/FunctionalSpacingReport.md", report.ToString(), new UTF8Encoding(false));
        }

        return new { apply, scanned = paths.Length, changed = replacements.Count, addedBlankLines = replacements.Sum(item => item.lines),
            codeTokensUnchanged = true, nonblankLinesUnchanged = true, files = replacements.Select(item => new { item.path, item.lines }).ToArray() };
    }

    private static string Tokens(SyntaxNode node) => string.Join("\u001f", node.DescendantTokens().Select(t => t.RawKind + ":" + t.Text));

    private static bool IsComment(string line) => line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*");

    private static bool IsGuard(StatementSyntax node) => node is IfStatementSyntax condition && condition.Else == null &&
        (condition.Statement is ReturnStatementSyntax || condition.Statement is ThrowStatementSyntax ||
         condition.Statement is ContinueStatementSyntax || condition.Statement is BreakStatementSyntax);

    private static bool IsControl(StatementSyntax node) => node is IfStatementSyntax || node is ForStatementSyntax ||
        node is ForEachStatementSyntax || node is WhileStatementSyntax || node is DoStatementSyntax ||
        node is SwitchStatementSyntax || node is TryStatementSyntax || node is UsingStatementSyntax || node is LockStatementSyntax;

    private static bool HasHeader(MemberDeclarationSyntax node) => node.AttributeLists.SelectMany(list => list.Attributes)
        .Any(attribute => attribute.Name.ToString().Contains("Header"));

    private static string Category(MemberDeclarationSyntax node)
    {
        if (node is FieldDeclarationSyntax field)
        {
            if (field.AttributeLists.SelectMany(list => list.Attributes).Any(a => a.Name.ToString().Contains("Serialize"))) return "settings";
            if (field.Modifiers.Any(t => t.IsKind(SyntaxKind.ConstKeyword) || t.IsKind(SyntaxKind.StaticKeyword))) return "constants";
            return "state";
        }

        if (node is PropertyDeclarationSyntax) return "property";
        if (node is BaseMethodDeclarationSyntax) return "method";
        if (node is BaseTypeDeclarationSyntax) return "type";
        return node.Kind().ToString();
    }
}
