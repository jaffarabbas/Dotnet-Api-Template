using System.Text.RegularExpressions;

namespace ApiTemplate.Security
{
    /// <summary>
    /// SECURITY: Validates SQL identifiers (table names, column names) to prevent SQL injection
    /// when dynamic SQL queries are constructed
    /// </summary>
    public static class SqlIdentifierValidator
    {
        // Only allow alphanumeric characters, underscores, and periods (for schema.table)
        private static readonly Regex ValidIdentifierPattern = new Regex(
            @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        // SQL Server reserved keywords that should not be used as identifiers
        private static readonly HashSet<string> ReservedKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC",
            "EXECUTE", "UNION", "WHERE", "FROM", "JOIN", "ORDER", "GROUP", "HAVING",
            "TABLE", "DATABASE", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER",
            "AND", "OR", "NOT", "NULL", "IS", "IN", "LIKE", "BETWEEN", "EXISTS",
            "ALL", "ANY", "SOME", "TOP", "DISTINCT", "AS", "INTO", "VALUES", "SET",
            "DECLARE", "BEGIN", "END", "IF", "ELSE", "WHILE", "RETURN", "CASE", "WHEN",
            "THEN", "CAST", "CONVERT", "COALESCE", "ISNULL", "NULLIF"
        };

        /// <summary>
        /// Validates that a SQL identifier is safe to use in dynamic SQL
        /// </summary>
        /// <param name="identifier">The table or column name to validate</param>
        /// <param name="parameterName">Name of the parameter (for error messages)</param>
        /// <exception cref="ArgumentException">Thrown if identifier is invalid or potentially malicious</exception>
        public static void ValidateIdentifier(string identifier, string parameterName = "identifier")
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException($"SQL identifier '{parameterName}' cannot be null or empty.", parameterName);
            }

            // Check length (SQL Server max identifier length is 128)
            if (identifier.Length > 128)
            {
                throw new ArgumentException(
                    $"SQL identifier '{parameterName}' is too long (max 128 characters). Value: {identifier}",
                    parameterName);
            }

            // Check for valid pattern
            if (!ValidIdentifierPattern.IsMatch(identifier))
            {
                throw new ArgumentException(
                    $"SQL identifier '{parameterName}' contains invalid characters. " +
                    $"Only alphanumeric characters, underscores, and periods are allowed. Value: {identifier}",
                    parameterName);
            }

            // Check for SQL injection attempts
            if (ContainsSqlInjectionPatterns(identifier))
            {
                throw new ArgumentException(
                    $"SQL identifier '{parameterName}' contains suspicious patterns that may indicate SQL injection. Value: {identifier}",
                    parameterName);
            }

            // Check for reserved keywords
            var identifierParts = identifier.Split('.');
            foreach (var part in identifierParts)
            {
                if (ReservedKeywords.Contains(part))
                {
                    throw new ArgumentException(
                        $"SQL identifier '{parameterName}' uses reserved keyword '{part}'. Use square brackets []{part}[] if intentional.",
                        parameterName);
                }
            }
        }

        /// <summary>
        /// Validates multiple identifiers at once
        /// </summary>
        public static void ValidateIdentifiers(params (string value, string name)[] identifiers)
        {
            foreach (var (value, name) in identifiers)
            {
                ValidateIdentifier(value, name);
            }
        }

        /// <summary>
        /// Safely quotes a SQL identifier with square brackets (SQL Server style)
        /// Use this when you need to use the identifier in dynamic SQL
        /// </summary>
        /// <param name="identifier">The validated identifier</param>
        /// <returns>Quoted identifier safe for use in SQL</returns>
        public static string QuoteIdentifier(string identifier)
        {
            ValidateIdentifier(identifier);

            // Handle schema.table format
            if (identifier.Contains('.'))
            {
                var parts = identifier.Split('.');
                return $"[{parts[0]}].[{parts[1]}]";
            }

            return $"[{identifier}]";
        }

        /// <summary>
        /// Checks for common SQL injection patterns
        /// </summary>
        private static bool ContainsSqlInjectionPatterns(string input)
        {
            var dangerousPatterns = new[]
            {
                "--",           // SQL comment
                "/*",           // Multi-line comment start
                "*/",           // Multi-line comment end
                ";",            // Statement separator
                "'",            // String delimiter
                "\"",           // String delimiter
                "xp_",          // Extended stored procedures
                "sp_",          // System stored procedures (some are dangerous)
                "0x",           // Hex values
                "char(",        // Character conversion
                "cast(",        // Type casting
                "convert(",     // Type conversion
                "@@",           // Global variables
                "waitfor",      // Time delay attack
                "delay",        // Time delay
                "benchmark",    // MySQL benchmark
                "sleep(",       // MySQL sleep
                "load_file",    // File reading
                "into outfile", // File writing
                "into dumpfile" // File writing
            };

            var lowerInput = input.ToLowerInvariant();
            return dangerousPatterns.Any(pattern => lowerInput.Contains(pattern.ToLowerInvariant()));
        }

        /// <summary>
        /// Validates ORDER BY clause to prevent SQL injection
        /// Allows: column_name, column_name ASC, column_name DESC
        /// </summary>
        public static void ValidateOrderByClause(string orderBy, string parameterName = "orderBy")
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                throw new ArgumentException($"ORDER BY clause '{parameterName}' cannot be null or empty.", parameterName);
            }

            // Pattern: column_name [ASC|DESC]
            var orderByPattern = new Regex(
                @"^[a-zA-Z_][a-zA-Z0-9_]*(\s+(ASC|DESC))?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            if (!orderByPattern.IsMatch(orderBy.Trim()))
            {
                throw new ArgumentException(
                    $"ORDER BY clause '{parameterName}' is invalid. " +
                    $"Only 'column_name', 'column_name ASC', or 'column_name DESC' are allowed. Value: {orderBy}",
                    parameterName);
            }

            // Check for SQL injection patterns
            if (ContainsSqlInjectionPatterns(orderBy))
            {
                throw new ArgumentException(
                    $"ORDER BY clause '{parameterName}' contains suspicious patterns. Value: {orderBy}",
                    parameterName);
            }
        }

        /// <summary>
        /// Validates a whitelist of allowed table names
        /// This is the most secure approach - explicitly list allowed tables
        /// </summary>
        public static void ValidateAgainstWhitelist(string tableName, IEnumerable<string> allowedTables)
        {
            if (!allowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Table name '{tableName}' is not in the allowed list. " +
                    $"Allowed tables: {string.Join(", ", allowedTables)}");
            }
        }
    }

    /// <summary>
    /// Extension methods for easier validation
    /// </summary>
    public static class SqlIdentifierValidatorExtensions
    {
        public static string ValidateAndQuote(this string identifier, string parameterName = "identifier")
        {
            SqlIdentifierValidator.ValidateIdentifier(identifier, parameterName);
            return SqlIdentifierValidator.QuoteIdentifier(identifier);
        }
    }
}
