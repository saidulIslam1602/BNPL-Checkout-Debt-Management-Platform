using YourCompanyBNPL.Common.Enums;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace YourCompanyBNPL.Notification.API.Infrastructure;

/// <summary>
/// Advanced template engine with support for conditionals, loops, and expressions
/// </summary>
public interface IAdvancedTemplateEngine
{
    Task<string> RenderAsync(string template, Dictionary<string, object> data, string language = "en", CancellationToken cancellationToken = default);
    Task<TemplateValidationResult> ValidateAsync(string template, CancellationToken cancellationToken = default);
    List<string> ExtractVariables(string template);
}

/// <summary>
/// Advanced template engine implementation
/// </summary>
public class AdvancedTemplateEngine : IAdvancedTemplateEngine
{
    private readonly ILogger<AdvancedTemplateEngine> _logger;
    private readonly TemplateEngineOptions _options;

    // Regex patterns for template syntax
    private static readonly Regex VariablePattern = new(@"\{\{\s*([a-zA-Z_][a-zA-Z0-9_\.]*)\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex ConditionalPattern = new(@"\{\%\s*if\s+(.+?)\s*\%\}(.*?)\{\%\s*endif\s*\%\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ConditionalElsePattern = new(@"\{\%\s*if\s+(.+?)\s*\%\}(.*?)\{\%\s*else\s*\%\}(.*?)\{\%\s*endif\s*\%\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex LoopPattern = new(@"\{\%\s*for\s+(\w+)\s+in\s+(\w+)\s*\%\}(.*?)\{\%\s*endfor\s*\%\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex FilterPattern = new(@"\{\{\s*([a-zA-Z_][a-zA-Z0-9_\.]*)\s*\|\s*(\w+)(?:\s*:\s*(.+?))?\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex FunctionPattern = new(@"\{\{\s*(\w+)\s*\((.*?)\)\s*\}\}", RegexOptions.Compiled);

    public AdvancedTemplateEngine(ILogger<AdvancedTemplateEngine> logger, TemplateEngineOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new TemplateEngineOptions();
    }

    public async Task<string> RenderAsync(string template, Dictionary<string, object> data, string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            var context = new TemplateContext(data, language);
            var result = await ProcessTemplateAsync(template, context, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template");
            throw new TemplateRenderException("Template rendering failed", ex.Message, ex);
        }
    }

    public async Task<TemplateValidationResult> ValidateAsync(string template, CancellationToken cancellationToken = default)
    {
        var result = new TemplateValidationResult { IsValid = true };

        try
        {
            // Check for balanced tags
            ValidateBalancedTags(template, result);

            // Check for valid syntax
            ValidateSyntax(template, result);

            // Check for circular references
            await ValidateCircularReferencesAsync(template, result, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    public List<string> ExtractVariables(string template)
    {
        var variables = new HashSet<string>();

        // Extract simple variables
        var matches = VariablePattern.Matches(template);
        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value);
        }

        // Extract variables from conditionals
        var conditionalMatches = ConditionalPattern.Matches(template);
        foreach (Match match in conditionalMatches)
        {
            var condition = match.Groups[1].Value;
            ExtractVariablesFromExpression(condition, variables);
        }

        // Extract variables from loops
        var loopMatches = LoopPattern.Matches(template);
        foreach (Match match in loopMatches)
        {
            variables.Add(match.Groups[2].Value); // Collection variable
        }

        return variables.ToList();
    }

    private async Task<string> ProcessTemplateAsync(string template, TemplateContext context, CancellationToken cancellationToken)
    {
        var result = template;

        // Process loops first (they can contain other constructs)
        result = await ProcessLoopsAsync(result, context, cancellationToken);

        // Process conditionals
        result = await ProcessConditionalsAsync(result, context, cancellationToken);

        // Process functions
        result = ProcessFunctions(result, context);

        // Process filters
        result = ProcessFilters(result, context);

        // Process simple variables
        result = ProcessVariables(result, context);

        return result;
    }

    private async Task<string> ProcessLoopsAsync(string template, TemplateContext context, CancellationToken cancellationToken)
    {
        var result = template;
        var matches = LoopPattern.Matches(template);

        foreach (Match match in matches.Cast<Match>().Reverse())
        {
            var itemVar = match.Groups[1].Value;
            var collectionVar = match.Groups[2].Value;
            var loopBody = match.Groups[3].Value;

            if (context.Data.TryGetValue(collectionVar, out var collectionObj))
            {
                var loopResult = await ProcessLoopAsync(itemVar, collectionObj, loopBody, context, cancellationToken);
                result = result.Substring(0, match.Index) + loopResult + result.Substring(match.Index + match.Length);
            }
            else
            {
                // Collection not found, remove the loop
                result = result.Substring(0, match.Index) + result.Substring(match.Index + match.Length);
            }
        }

        return result;
    }

    private async Task<string> ProcessLoopAsync(string itemVar, object collection, string loopBody, TemplateContext context, CancellationToken cancellationToken)
    {
        var result = new List<string>();

        if (collection is IEnumerable<object> enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                var loopContext = context.CreateChildContext();
                loopContext.Data[itemVar] = item;
                loopContext.Data[$"{itemVar}_index"] = index;
                loopContext.Data[$"{itemVar}_first"] = index == 0;

                var processedBody = await ProcessTemplateAsync(loopBody, loopContext, cancellationToken);
                result.Add(processedBody);
                index++;
            }
        }
        else if (collection is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in jsonElement.EnumerateArray())
            {
                var loopContext = context.CreateChildContext();
                loopContext.Data[itemVar] = item;
                loopContext.Data[$"{itemVar}_index"] = index;
                loopContext.Data[$"{itemVar}_first"] = index == 0;

                var processedBody = await ProcessTemplateAsync(loopBody, loopContext, cancellationToken);
                result.Add(processedBody);
                index++;
            }
        }

        return string.Join("", result);
    }

    private async Task<string> ProcessConditionalsAsync(string template, TemplateContext context, CancellationToken cancellationToken)
    {
        var result = template;

        // Process if-else conditionals first
        var elseMatches = ConditionalElsePattern.Matches(template);
        foreach (Match match in elseMatches.Cast<Match>().Reverse())
        {
            var condition = match.Groups[1].Value;
            var trueContent = match.Groups[2].Value;
            var falseContent = match.Groups[3].Value;

            var conditionResult = EvaluateCondition(condition, context);
            var selectedContent = conditionResult ? trueContent : falseContent;
            var processedContent = await ProcessTemplateAsync(selectedContent, context, cancellationToken);

            result = result.Substring(0, match.Index) + processedContent + result.Substring(match.Index + match.Length);
        }

        // Process simple if conditionals
        var ifMatches = ConditionalPattern.Matches(result);
        foreach (Match match in ifMatches.Cast<Match>().Reverse())
        {
            var condition = match.Groups[1].Value;
            var content = match.Groups[2].Value;

            var conditionResult = EvaluateCondition(condition, context);
            var processedContent = conditionResult ? await ProcessTemplateAsync(content, context, cancellationToken) : "";

            result = result.Substring(0, match.Index) + processedContent + result.Substring(match.Index + match.Length);
        }

        return result;
    }

    private bool EvaluateCondition(string condition, TemplateContext context)
    {
        try
        {
            // Simple condition evaluation
            condition = condition.Trim();

            // Handle negation
            var isNegated = condition.StartsWith("not ");
            if (isNegated)
            {
                condition = condition.Substring(4).Trim();
            }

            // Handle comparison operators
            var comparisonOperators = new[] { "==", "!=", ">=", "<=", ">", "<" };
            foreach (var op in comparisonOperators)
            {
                if (condition.Contains(op))
                {
                    var parts = condition.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var left = GetVariableValue(parts[0].Trim(), context);
                        var right = GetVariableValue(parts[1].Trim(), context);
                        var result = CompareValues(left, right, op);
                        return isNegated ? !result : result;
                    }
                }
            }

            // Handle logical operators
            if (condition.Contains(" and "))
            {
                var parts = condition.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
                var result = parts.All(part => EvaluateCondition(part.Trim(), context));
                return isNegated ? !result : result;
            }

            if (condition.Contains(" or "))
            {
                var parts = condition.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
                var result = parts.Any(part => EvaluateCondition(part.Trim(), context));
                return isNegated ? !result : result;
            }

            // Simple boolean evaluation
            var value = GetVariableValue(condition, context);
            var boolResult = IsTruthy(value);
            return isNegated ? !boolResult : boolResult;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate condition: {Condition}", condition);
            return false;
        }
    }

    private bool CompareValues(object? left, object? right, string op)
    {
        return op switch
        {
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            ">" => CompareNumeric(left, right) > 0,
            "<" => CompareNumeric(left, right) < 0,
            ">=" => CompareNumeric(left, right) >= 0,
            "<=" => CompareNumeric(left, right) <= 0,
            _ => false
        };
    }

    private int CompareNumeric(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        if (double.TryParse(left.ToString(), out var leftNum) && double.TryParse(right.ToString(), out var rightNum))
        {
            return leftNum.CompareTo(rightNum);
        }

        return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            double d => d != 0.0,
            decimal dec => dec != 0m,
            IEnumerable<object> enumerable => enumerable.Any(),
            JsonElement json => json.ValueKind != JsonValueKind.Null && json.ValueKind != JsonValueKind.Undefined,
            _ => true
        };
    }

    private string ProcessFunctions(string template, TemplateContext context)
    {
        var result = template;
        var matches = FunctionPattern.Matches(template);

        foreach (Match match in matches.Cast<Match>().Reverse())
        {
            var functionName = match.Groups[1].Value;
            var argsString = match.Groups[2].Value;
            var args = ParseFunctionArgs(argsString, context);

            var functionResult = ExecuteFunction(functionName, args, context);
            result = result.Substring(0, match.Index) + functionResult + result.Substring(match.Index + match.Length);
        }

        return result;
    }

    private string ProcessFilters(string template, TemplateContext context)
    {
        var result = template;
        var matches = FilterPattern.Matches(template);

        foreach (Match match in matches.Cast<Match>().Reverse())
        {
            var variableName = match.Groups[1].Value;
            var filterName = match.Groups[2].Value;
            var filterArgs = match.Groups[3].Success ? match.Groups[3].Value : null;

            var value = GetVariableValue(variableName, context);
            var filteredValue = ApplyFilter(value, filterName, filterArgs, context);

            result = result.Substring(0, match.Index) + filteredValue + result.Substring(match.Index + match.Length);
        }

        return result;
    }

    private string ProcessVariables(string template, TemplateContext context)
    {
        return VariablePattern.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            var value = GetVariableValue(variableName, context);
            return FormatValue(value, context);
        });
    }

    private object? GetVariableValue(string variableName, TemplateContext context)
    {
        // Handle quoted strings
        if ((variableName.StartsWith("'") && variableName.EndsWith("'")) ||
            (variableName.StartsWith("\"") && variableName.EndsWith("\"")))
        {
            return variableName.Substring(1, variableName.Length - 2);
        }

        // Handle numbers
        if (double.TryParse(variableName, out var numValue))
        {
            return numValue;
        }

        // Handle boolean literals
        if (variableName.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (variableName.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        // Handle nested properties (e.g., user.name)
        var parts = variableName.Split('.');
        object? current = null;

        if (context.Data.TryGetValue(parts[0], out current))
        {
            for (int i = 1; i < parts.Length && current != null; i++)
            {
                current = GetPropertyValue(current, parts[i]);
            }
        }

        return current;
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj is Dictionary<string, object> dict)
        {
            return dict.TryGetValue(propertyName, out var value) ? value : null;
        }

        if (obj is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            return json.TryGetProperty(propertyName, out var prop) ? prop : null;
        }

        // Use reflection for other objects
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    private string FormatValue(object? value, TemplateContext context)
    {
        return value switch
        {
            null => "",
            string s => s,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.GetCultureInfo(context.Language)),
            decimal d => d.ToString("N2", CultureInfo.GetCultureInfo(context.Language)),
            double d => d.ToString("N2", CultureInfo.GetCultureInfo(context.Language)),
            float f => f.ToString("N2", CultureInfo.GetCultureInfo(context.Language)),
            JsonElement json => FormatJsonElement(json),
            _ => value.ToString() ?? ""
        };
    }

    private string FormatJsonElement(JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString() ?? "",
            JsonValueKind.Number => json.GetDecimal().ToString("N2"),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => json.ToString()
        };
    }

    private string ApplyFilter(object? value, string filterName, string? args, TemplateContext context)
    {
        var stringValue = FormatValue(value, context);

        return filterName.ToLowerInvariant() switch
        {
            "upper" => stringValue.ToUpperInvariant(),
            "lower" => stringValue.ToLowerInvariant(),
            "title" => CultureInfo.GetCultureInfo(context.Language).TextInfo.ToTitleCase(stringValue.ToLower()),
            "truncate" => TruncateString(stringValue, args),
            "default" => string.IsNullOrEmpty(stringValue) ? (args ?? "") : stringValue,
            "currency" => FormatCurrency(value, context.Language),
            "date" => FormatDate(value, args, context.Language),
            "escape" => System.Web.HttpUtility.HtmlEncode(stringValue),
            "nl2br" => stringValue.Replace("\n", "<br>"),
            _ => stringValue
        };
    }

    private string TruncateString(string value, string? lengthArg)
    {
        if (string.IsNullOrEmpty(lengthArg) || !int.TryParse(lengthArg, out var length))
            return value;

        return value.Length <= length ? value : value.Substring(0, length) + "...";
    }

    private string FormatCurrency(object? value, string language)
    {
        if (value == null) return "";

        if (decimal.TryParse(value.ToString(), out var amount))
        {
            var culture = CultureInfo.GetCultureInfo(language);
            return amount.ToString("C", culture);
        }

        return value.ToString() ?? "";
    }

    private string FormatDate(object? value, string? format, string language)
    {
        if (value == null) return "";

        DateTime date;
        if (value is DateTime dt)
        {
            date = dt;
        }
        else if (DateTime.TryParse(value.ToString(), out date))
        {
            // Successfully parsed
        }
        else
        {
            return value.ToString() ?? "";
        }

        var culture = CultureInfo.GetCultureInfo(language);
        return date.ToString(format ?? "yyyy-MM-dd", culture);
    }

    private string ExecuteFunction(string functionName, object?[] args, TemplateContext context)
    {
        return functionName.ToLowerInvariant() switch
        {
            "now" => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            "today" => DateTime.Today.ToString("yyyy-MM-dd"),
            "random" => Random.Shared.Next(1, 1000).ToString(),
            "count" => args.Length > 0 && args[0] is IEnumerable<object> enumerable ? enumerable.Count().ToString() : "0",
            _ => $"[Unknown function: {functionName}]"
        };
    }

    private object?[] ParseFunctionArgs(string argsString, TemplateContext context)
    {
        if (string.IsNullOrWhiteSpace(argsString))
            return Array.Empty<object>();

        var args = argsString.Split(',').Select(arg => GetVariableValue(arg.Trim(), context)).ToArray();
        return args;
    }

    private void ExtractVariablesFromExpression(string expression, HashSet<string> variables)
    {
        var matches = Regex.Matches(expression, @"\b([a-zA-Z_][a-zA-Z0-9_\.]*)\b");
        foreach (Match match in matches)
        {
            var variable = match.Groups[1].Value;
            if (!IsReservedWord(variable))
            {
                variables.Add(variable);
            }
        }
    }

    private bool IsReservedWord(string word)
    {
        var reserved = new[] { "and", "or", "not", "true", "false", "null", "if", "else", "endif", "for", "endfor", "in" };
        return reserved.Contains(word.ToLowerInvariant());
    }

    private void ValidateBalancedTags(string template, TemplateValidationResult result)
    {
        var ifCount = Regex.Matches(template, @"\{\%\s*if\s+").Count;
        var endifCount = Regex.Matches(template, @"\{\%\s*endif\s*\%\}").Count;
        var forCount = Regex.Matches(template, @"\{\%\s*for\s+").Count;
        var endforCount = Regex.Matches(template, @"\{\%\s*endfor\s*\%\}").Count;

        if (ifCount != endifCount)
        {
            result.IsValid = false;
            result.Errors.Add($"Unbalanced if/endif tags: {ifCount} if, {endifCount} endif");
        }

        if (forCount != endforCount)
        {
            result.IsValid = false;
            result.Errors.Add($"Unbalanced for/endfor tags: {forCount} for, {endforCount} endfor");
        }
    }

    private void ValidateSyntax(string template, TemplateValidationResult result)
    {
        // Validate variable syntax
        var invalidVariables = Regex.Matches(template, @"\{\{[^}]*\}\}")
            .Cast<Match>()
            .Where(m => !VariablePattern.IsMatch(m.Value) && !FilterPattern.IsMatch(m.Value) && !FunctionPattern.IsMatch(m.Value))
            .Select(m => m.Value);

        foreach (var invalid in invalidVariables)
        {
            result.Warnings.Add($"Invalid variable syntax: {invalid}");
        }
    }

    private async Task ValidateCircularReferencesAsync(string template, TemplateValidationResult result, CancellationToken cancellationToken)
    {
        // This is a simplified check - in a real implementation, you'd track template includes/extends
        await Task.CompletedTask;
    }
}

/// <summary>
/// Template context for rendering
/// </summary>
public class TemplateContext
{
    public Dictionary<string, object> Data { get; }
    public string Language { get; }

    public TemplateContext(Dictionary<string, object> data, string language)
    {
        Data = new Dictionary<string, object>(data);
        Language = language;
    }

    public TemplateContext CreateChildContext()
    {
        return new TemplateContext(new Dictionary<string, object>(Data), Language);
    }
}

/// <summary>
/// Template validation result
/// </summary>
public class TemplateValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}

/// <summary>
/// Template engine options
/// </summary>
public class TemplateEngineOptions
{
    public int MaxRenderDepth { get; set; } = 10;
    public int MaxLoopIterations { get; set; } = 1000;
    public bool StrictMode { get; set; } = false;
}

/// <summary>
/// Template render exception
/// </summary>
public class TemplateRenderException : Exception
{
    public string TemplateName { get; }
    public string RenderError { get; }

    public TemplateRenderException(string templateName, string renderError, Exception? innerException = null)
        : base($"Failed to render template '{templateName}': {renderError}", innerException)
    {
        TemplateName = templateName;
        RenderError = renderError;
    }
}