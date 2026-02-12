using System;
using System.Linq;
using Dapper;

namespace Orchestrator.Domain
{
    /// <summary>
    /// Extension methods for string manipulation
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to PascalCase
        /// </summary>
        /// <param name="str">The input string</param>
        /// <returns>The string in PascalCase</returns>
        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Split by underscore and capitalize each word
            var words = str.Split('_');
            var result = string.Join("", words.Select(word => 
            {
                if (word.Length == 0) return "";
                return char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }));
            
            return result;
        }
    }

    /// <summary>
    /// Dapper configuration helper
    /// </summary>
    public static class DapperConfiguration
    {
        /// <summary>
        /// Configures Dapper to map snake_case columns to PascalCase properties for a given type
        /// </summary>
        /// <typeparam name="T">The type to configure mapping for</typeparam>
        public static void ConfigureSnakeCaseMapping<T>()
        {
            SqlMapper.SetTypeMap(typeof(T), new CustomPropertyTypeMap(typeof(T), (type, columnName) =>
            {
                var pascalCaseName = columnName.ToPascalCase();
                return type.GetProperty(pascalCaseName)!;
            }));
        }
    }
} 