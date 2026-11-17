namespace _Scripts._Core
{
    public static class CodeFormat
    {
        public const string EnumFormat = 
@"
namespace {0} 
{{
    public enum {1}
    {{
        {2}
    }}
}}
";
    }
}