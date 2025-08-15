using System.Text.Json;

namespace ShopifyLib.Utils
{
    /// <summary>
    /// Helper class to provide consistent JSON naming policy across different .NET versions
    /// </summary>
    public static class JsonNamingPolicyHelper
    {
        /// <summary>
        /// Gets a snake_case naming policy that works across all target frameworks
        /// </summary>
        public static JsonNamingPolicy SnakeCaseLower
        {
            get
            {
#if NET48
                return new SnakeCaseNamingPolicy();
#else
                return JsonNamingPolicy.SnakeCaseLower;
#endif
            }
        }

#if NET48
        /// <summary>
        /// Custom snake_case naming policy for .NET Framework 4.8
        /// </summary>
        private class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                var result = "";
                for (int i = 0; i < name.Length; i++)
                {
                    if (i > 0 && char.IsUpper(name[i]))
                    {
                        result += "_";
                    }
                    result += char.ToLower(name[i]);
                }
                return result;
            }
        }
#endif
    }
} 