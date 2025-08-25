using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using copiloty_stats_viewer.Models;

namespace copiloty_stats_viewer.Services;

public sealed class DataService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new DateOnlyJsonConverter() }
    };

    private List<CopilotRecord> _records = new();

    // Filters
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public HashSet<string> SelectedUsers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedFeatures { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedModels { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    // Total licensed users for accurate adoption rate calculation
    public int TotalLicensedUsers { get; set; } = 0;

    public void SetTotalLicensedUsers(int count)
    {
        TotalLicensedUsers = count;
    }

    public IReadOnlyList<CopilotRecord> Records => _records;

    // Language name mapping for user-friendly display
    public static string GetFriendlyLanguageName(string language)
    {
        return language?.ToLowerInvariant() switch
        {
            "javascript" => "JavaScript",
            "typescript" => "TypeScript", 
            "typescriptreact" => "TypeScript React",
            "javascriptreact" => "JavaScript React",
            "powershell" => "PowerShell",
            "python" => "Python",
            "csharp" => "C#",
            "java" => "Java",
            "cpp" => "C++",
            "c" => "C",
            "php" => "PHP",
            "ruby" => "Ruby",
            "go" => "Go",
            "rust" => "Rust",
            "swift" => "Swift",
            "kotlin" => "Kotlin",
            "scala" => "Scala",
            "html" => "HTML",
            "css" => "CSS",
            "scss" => "SCSS",
            "less" => "LESS",
            "json" => "JSON",
            "xml" => "XML",
            "yaml" => "YAML",
            "yml" => "YAML",
            "markdown" => "Markdown",
            "sql" => "SQL",
            "dockerfile" => "Dockerfile",
            "sh" => "Shell Script",
            "bash" => "Bash",
            "zsh" => "Zsh",
            "fish" => "Fish",
            "dotenv" => ".env",
            "pip-requirements" => "Requirements.txt",
            "github-actions-workflow" => "GitHub Actions",
            "oracle-sql" => "Oracle SQL",
            "mermaid" => "Mermaid",
            "vue" => "Vue.js",
            "svelte" => "Svelte",
            "angular" => "Angular",
            "react" => "React",
            "unknown" => "Unknown",
            _ => language ?? "Unknown"
        };
    }

    // Model name mapping for user-friendly display
    public static string GetFriendlyModelName(string model)
    {
        return model?.ToLowerInvariant() switch
        {
            "gpt-4" => "GPT-4",
            "gpt-4.1" => "GPT-4.1",
            "gpt-4o" => "GPT-4o",
            "claude-3.5-sonnet" => "Claude 3.5 Sonnet",
            "claude-3.7-sonnet" => "Claude 3.7 Sonnet",
            "claude-4.0-sonnet" => "Claude 4.0 Sonnet",
            "claude-sonnet" => "Claude Sonnet",
            "claude-haiku" => "Claude Haiku",
            "claude-opus" => "Claude Opus",
            "unknown" => "Unknown",
            _ => model ?? "Unknown"
        };
    }

    // Feature name mapping for user-friendly display
    public static string GetFriendlyFeatureName(string feature)
    {
        return feature?.ToLowerInvariant() switch
        {
            "code_completion" => "Code Completion",
            "chat" => "Chat Assistant", 
            "chat_inline" => "Inline Chat",
            "chat_panel_ask_mode" => "Chat Panel - Ask Mode",
            "chat_panel_edit_mode" => "Chat Panel - Edit Mode",
            "chat_panel_agent_mode" => "Chat Panel - Agent Mode", 
            "chat_panel_unknown_mode" => "Chat Panel - Unknown Mode",
            "chat_panel_custom_mode" => "Chat Panel - Custom Mode",
            "code_generation" => "Code Generation",
            "code_review" => "Code Review",
            "code_explanation" => "Code Explanation",
            "test_generation" => "Test Generation",
            "documentation" => "Documentation",
            "refactoring" => "Refactoring",
            _ => feature ?? "Unknown"
        };
    }

    public async Task LoadNdjsonAsync(Stream stream, IProgress<(int Records, long BytesRead)>? progress = null, CancellationToken ct = default)
    {
        _records.Clear();
        
        // Read entire stream into memory for much faster processing
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        var content = Encoding.UTF8.GetString(memoryStream.ToArray());
        var totalBytes = memoryStream.Length;
        
        // Split into lines and process in bulk
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var records = new List<CopilotRecord>(lines.Length);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            try
            {
                var rec = JsonSerializer.Deserialize<CopilotRecord>(line, _jsonOptions);
                if (rec != null)
                {
                    records.Add(rec);
                }
            }
            catch
            {
                // skip bad lines
            }

            // Report progress every 1000 lines for better performance
            if (progress != null && (i % 1000 == 0))
            {
                // Estimate bytes processed based on line position
                var estimatedBytes = (long)((double)i / lines.Length * totalBytes);
                progress.Report((records.Count, estimatedBytes));
            }
        }

        _records.AddRange(records);
        
        // Final progress update
        progress?.Report((records.Count, totalBytes));
    }

    public IEnumerable<CopilotRecord> GetFiltered()
    {
        IEnumerable<CopilotRecord> q = _records;
        if (FromDate is { } fd) q = q.Where(r => r.Day >= fd);
        if (ToDate is { } td) q = q.Where(r => r.Day <= td);
        if (SelectedUsers.Count > 0) q = q.Where(r => SelectedUsers.Contains(r.UserLogin));
        if (SelectedFeatures.Count > 0) q = q.Where(r => r.TotalsByFeature.Any(f => SelectedFeatures.Contains(f.Feature)));
        if (SelectedModels.Count > 0) q = q.Where(r => r.TotalsByModelFeature.Any(m => SelectedModels.Contains(m.Model)) || r.TotalsByLanguageModel.Any(m => SelectedModels.Contains(m.Model)));
        return q;
    }

    // Aggregations for charts/KPIs
    public sealed record TimeSeriesPoint(DateOnly Day, int Interactions, int Generations, int Acceptances);

    public IEnumerable<TimeSeriesPoint> GetTimeSeries()
    {
        return GetFiltered()
            .GroupBy(r => r.Day)
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(
                g.Key,
                g.Sum(r => r.UserInitiatedInteractionCount),
                g.Sum(r => r.CodeGenerationActivityCount),
                g.Sum(r => r.CodeAcceptanceActivityCount)
            ));
    }

    public IEnumerable<(string User, int Generations, int Acceptances)> TopUsers(int top = 10)
    {
        return GetFiltered()
            .GroupBy(r => r.UserLogin)
            .Select(g => (User: g.Key, Generations: g.Sum(r => r.CodeGenerationActivityCount), Acceptances: g.Sum(r => r.CodeAcceptanceActivityCount)))
            .OrderByDescending(t => t.Generations)
            .Take(top);
    }

    public IEnumerable<(string Feature, int Generations)> FeatureMix()
    {
        return GetFiltered()
            .SelectMany(r => r.TotalsByFeature)
            .GroupBy(f => f.Feature)
            .Select(g => (Feature: g.Key, Generations: g.Sum(f => f.CodeGenerationActivityCount)))
            .OrderByDescending(t => t.Generations);
    }

    public IEnumerable<(string Model, int Generations)> ModelMix()
    {
        return GetFiltered()
            .SelectMany(r => r.TotalsByModelFeature)
            .GroupBy(m => string.IsNullOrWhiteSpace(m.Model) ? "Unknown" : m.Model)
            .Select(g => (Model: g.Key, Generations: g.Sum(m => m.CodeGenerationActivityCount)))
            .OrderByDescending(t => t.Generations);
    }

    public (string Model, int Generations) GetMostUsedModel()
    {
        return ModelMix().FirstOrDefault();
    }

    public IEnumerable<(DateOnly Day, Dictionary<string, int> ModelUsage)> GetModelUsagePerDay()
    {
        var filtered = GetFiltered();
        
        var result = filtered
            .GroupBy(r => r.Day)
            .Select(dayGroup => new
            {
                Day = dayGroup.Key,
                ModelUsage = dayGroup
                    .SelectMany(r => r.TotalsByModelFeature)
                    .Where(tm => !string.IsNullOrWhiteSpace(tm.Model) && tm.CodeGenerationActivityCount > 0) // Filter out empty models and zero activity
                    .GroupBy(tm => tm.Model)
                    .ToDictionary(
                        g => GetFriendlyModelName(g.Key),
                        g => g.Sum(tm => tm.CodeGenerationActivityCount)
                    )
            })
            .Where(x => x.ModelUsage.Any()) // Only include days with actual model usage
            .OrderBy(x => x.Day)
            .Select(x => (x.Day, x.ModelUsage));

        return result;
    }

    public IEnumerable<(DateOnly Day, Dictionary<string, int> LanguageUsage)> GetLanguageUsagePerDay()
    {
        var filtered = GetFiltered();
        
        var result = filtered
            .GroupBy(r => r.Day)
            .Select(dayGroup => new
            {
                Day = dayGroup.Key,
                LanguageUsage = dayGroup
                    .SelectMany(r => r.TotalsByLanguageFeature)
                    .Where(lf => !string.IsNullOrWhiteSpace(lf.Language) && lf.CodeGenerationActivityCount > 0) // Filter out empty languages and zero activity
                    .GroupBy(lf => lf.Language)
                    .ToDictionary(
                        g => GetFriendlyLanguageName(g.Key),
                        g => g.Sum(lf => lf.CodeGenerationActivityCount)
                    )
            })
            .Where(x => x.LanguageUsage.Any()) // Only include days with actual language usage
            .OrderBy(x => x.Day)
            .Select(x => (x.Day, x.LanguageUsage));

        return result;
    }

    public IEnumerable<(string Language, string Model, int AcceptedSuggestions)> GetModelAcceptanceByLanguage()
    {
        var filtered = GetFiltered();
        
        var result = filtered
            .SelectMany(r => r.TotalsByLanguageModel)
            .Where(lm => !string.IsNullOrWhiteSpace(lm.Language) && 
                        !string.IsNullOrWhiteSpace(lm.Model) && 
                        lm.CodeAcceptanceActivityCount > 0) // Filter out empty values and zero activity
            .GroupBy(lm => new {
                Language = GetFriendlyLanguageName(lm.Language),
                Model = GetFriendlyModelName(lm.Model)
            })
            .Select(g => (
                Language: g.Key.Language,
                Model: g.Key.Model,
                AcceptedSuggestions: g.Sum(lm => lm.CodeAcceptanceActivityCount)
            ))
            .OrderBy(x => x.Language)
            .ThenByDescending(x => x.AcceptedSuggestions);

        return result;
    }

    public string GetMostUsedLanguageForUser(string userLogin)
    {
        var userRecords = GetFiltered().Where(r => r.UserLogin == userLogin);
        
        var languageUsage = userRecords
            .SelectMany(r => r.TotalsByLanguageFeature)
            .Where(lf => !string.IsNullOrEmpty(lf.Language) && lf.CodeGenerationActivityCount > 0) // Filter out empty languages and zero activity
            .GroupBy(lf => lf.Language)
            .Select(g => new { Language = g.Key, Generations = g.Sum(lf => lf.CodeGenerationActivityCount) })
            .OrderByDescending(x => x.Generations)
            .FirstOrDefault();

        return GetFriendlyLanguageName(languageUsage?.Language ?? "Unknown");
    }

    public string GetMostUsedModelForUser(string userLogin)
    {
        var userRecords = GetFiltered().Where(r => r.UserLogin == userLogin);
        
        var modelUsage = userRecords
            .SelectMany(r => r.TotalsByModelFeature)
            .Where(mf => !string.IsNullOrEmpty(mf.Model) && mf.CodeGenerationActivityCount > 0) // Filter out empty models and zero activity
            .GroupBy(mf => mf.Model)
            .Select(g => new { Model = g.Key, Generations = g.Sum(mf => mf.CodeGenerationActivityCount) })
            .OrderByDescending(x => x.Generations)
            .FirstOrDefault();

        return GetFriendlyModelName(modelUsage?.Model ?? "Unknown");
    }

    public sealed record AdoptionStats(int ActiveUsers, int UsingChat, int UsingInline, int UsingCompletions);

    public AdoptionStats GetAdoption()
    {
        var filtered = GetFiltered().ToList();
        int activeUsers = filtered.Select(r => r.UserLogin).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        int usingChat = filtered.Count(r => r.UsedChat);
        int usingInline = filtered.Count(r => r.TotalsByFeature.Any(f => string.Equals(f.Feature, "chat_inline", StringComparison.OrdinalIgnoreCase)));
        int usingCompletions = filtered.Count(r => r.TotalsByFeature.Any(f => string.Equals(f.Feature, "code_completion", StringComparison.OrdinalIgnoreCase)));
        return new AdoptionStats(activeUsers, usingChat, usingInline, usingCompletions);
    }

    /// <summary>
    /// Represents total metrics with Activity-Based acceptance rate calculation.
    /// The AcceptanceRate property uses activity session counts (not individual suggestions).
    /// This differs from GitHub's official Suggestion-Based metrics which count individual suggestions.
    /// </summary>
    public sealed record Totals(int Interactions, int Generations, int Acceptances)
    {
        /// <summary>
        /// Activity-Based Acceptance Rate: Percentage of code generation activities that resulted in acceptance activities.
        /// Formula: Acceptances / Generations
        /// Note: This typically differs from GitHub's Suggestion-Based rate which counts individual suggestions.
        /// </summary>
        public double AcceptanceRate => Generations > 0 ? (double)Acceptances / Generations : 0d;
    }

    public Totals GetTotals()
    {
        var filtered = GetFiltered();
        int interactions = filtered.Sum(r => r.UserInitiatedInteractionCount);
        int generations = filtered.Sum(r => r.CodeGenerationActivityCount);
        int acceptances = filtered.Sum(r => r.CodeAcceptanceActivityCount);
        return new Totals(interactions, generations, acceptances);
    }

    // Configurable parameters for metrics
    public double SecondsPerAcceptance { get; set; } = 30.0; // Default 30 seconds saved per acceptance
    public double PowerUserAcceptanceThreshold { get; set; } = 0.3; // 30% acceptance rate
    public int PowerUserActiveDaysThreshold { get; set; } = 3; // 3+ active days
    public int EngagementThreshold { get; set; } = 5; // 5+ acceptances for engagement

    /// <summary>
    /// AI Development Enablement Index metrics with Activity-Based calculations.
    /// AcceptanceRate uses activity session counts, not individual suggestion counts.
    /// </summary>
    public sealed record AIDEIMetrics(
        double AdoptionRate,
        /// <summary>
        /// Activity-Based Acceptance Rate for AIDEI calculation
        /// </summary>
        double AcceptanceRate, 
        double LicensedVsEngagedRate,
        double UsageRate,
        double AIDEIScore
    );

    // Comprehensive engineering metrics
    public sealed record EngineeringMetrics(
        // License and adoption metrics
        double LicenseUtilization,
        int UnusedSeats,
        double EngagedUsersPercent,
        double UsageRate,
        
        // Performance and quality metrics
        double MedianAcceptanceRate,
        double AcceptancesPerActiveUserPerDay,
        double PowerUsersPercent,
        
        // Feature usage metrics
        double InlineSharePercent,
        double ChatAdoptionPercent,
        
        // Model and distribution metrics
        double ModelLeaderMargin,
        double ConcentrationIndex,
        
        // Growth and efficiency metrics
        double RampRateUsersPerWeek,
        double TimeToFirstValueDays,
        double LanguageCoveragePercent,
        double EstimatedTimeSavedHours
    );

    public AIDEIMetrics GetAIDEI()
    {
        var filtered = GetFiltered().ToList();
        
        // Get date range for working day calculations (used by multiple metrics)
        var dateRange = filtered.GroupBy(r => r.Day).Select(g => g.Key).ToList();
        var workingDays = 0;
        if (dateRange.Count > 0)
        {
            var minDate = dateRange.Min();
            var maxDate = dateRange.Max();
            
            // Calculate working weekdays in the date range (exclude weekends)
            for (var date = minDate; date <= maxDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
        }
        
        // Adoption Rate: Users who have any Copilot activity vs Total Licensed Users
        var usersWithActivity = filtered.Where(r => r.CodeGenerationActivityCount > 0 || r.UserInitiatedInteractionCount > 0)
            .Select(r => r.UserLogin).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var adoptionRate = TotalLicensedUsers > 0 ? (double)usersWithActivity / TotalLicensedUsers : 0;

        // Acceptance Rate: Accepted suggestions vs generated suggestions
        var totalGenerations = filtered.Sum(r => r.CodeGenerationActivityCount);
        var totalAcceptances = filtered.Sum(r => r.CodeAcceptanceActivityCount);
        var acceptanceRate = totalGenerations > 0 ? (double)totalAcceptances / totalGenerations : 0;

        // Licensed vs Engaged Rate: Users with meaningful daily engagement vs Total Licensed Users
        double licensedVsEngagedRate = 0.0;
        if (TotalLicensedUsers > 0 && workingDays > 0)
        {
            // Get users who have meaningful engagement (>threshold activities on at least 2 days)
            var meaningfullyEngagedUsers = filtered.GroupBy(r => r.UserLogin)
                .Where(g => g.Count(r => (r.UserInitiatedInteractionCount + r.CodeGenerationActivityCount) > EngagementThreshold) >= 2)
                .Select(g => g.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            
            licensedVsEngagedRate = (double)meaningfullyEngagedUsers / TotalLicensedUsers;
        }

        // Usage Rate: Consistent daily usage across working days
        double usageRate = 0.0;
        if (workingDays > 0)
        {
            // Get unique users who had activity and their active days (only count days with >3 activities)
            var userActivityDays = filtered.GroupBy(r => r.UserLogin)
                .Select(g => new { 
                    User = g.Key, 
                    ActiveDays = g.Count(r => (r.UserInitiatedInteractionCount + r.CodeGenerationActivityCount) > 3),
                    TotalActivity = g.Sum(r => r.UserInitiatedInteractionCount + r.CodeGenerationActivityCount)
                })
                .Where(u => u.TotalActivity > 0 && u.ActiveDays > 0)
                .ToList();
            
            if (userActivityDays.Count > 0)
            {
                // Calculate average percentage of working days each active user used Copilot
                var avgDaysUsedPerUser = userActivityDays.Average(u => u.ActiveDays);
                usageRate = Math.Min(1.0, avgDaysUsedPerUser / workingDays);
            }
        }

        // AIDEI Score calculation
        var aideiScore = (adoptionRate * 0.4) + (acceptanceRate * 0.4) + (licensedVsEngagedRate * 0.2);

        return new AIDEIMetrics(adoptionRate, acceptanceRate, licensedVsEngagedRate, usageRate, aideiScore);
    }

    public EngineeringMetrics GetEngineeringMetrics()
    {
        var filtered = GetFiltered().ToList();
        
        if (!filtered.Any())
        {
            return new EngineeringMetrics(0, TotalLicensedUsers, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        // Get basic counts
        var activeUsers = filtered.Select(r => r.UserLogin).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var activeUserCount = activeUsers.Count;
        
        // Date range and working days
        var dateRange = filtered.GroupBy(r => r.Day).Select(g => g.Key).ToList();
        var workingDays = 0;
        if (dateRange.Count > 0)
        {
            var minDate = dateRange.Min();
            var maxDate = dateRange.Max();
            for (var date = minDate; date <= maxDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                    workingDays++;
            }
        }
        
        // 1. License Utilization % and Unused Seats
        var licenseUtilization = TotalLicensedUsers > 0 ? (double)activeUserCount / TotalLicensedUsers : 0;
        var unusedSeats = Math.Max(0, TotalLicensedUsers - activeUserCount);
        
        // 2. Engaged Users % (meaningful engagement: >=threshold activities on >=2 days)
        var meaningfulUsers = 0;
        if (workingDays > 0)
        {
            var userActivityDays = filtered
                .GroupBy(r => r.UserLogin)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(r => r.Day)
                          .Count(dayGroup => dayGroup.Sum(r => r.UserInitiatedInteractionCount + r.CodeGenerationActivityCount) >= EngagementThreshold)
                );
            meaningfulUsers = userActivityDays.Count(kvp => kvp.Value >= 2);
        }
        var engagedUsersPercent = TotalLicensedUsers > 0 ? (double)meaningfulUsers / TotalLicensedUsers : 0;
        
        // 3. Usage Rate % (already calculated in AIDEI, recalculate here)
        var usageRate = 0.0;
        if (workingDays > 0 && meaningfulUsers > 0)
        {
            var totalActiveDays = filtered.GroupBy(r => new { r.UserLogin, r.Day }).Count();
            usageRate = (double)totalActiveDays / (meaningfulUsers * workingDays);
        }
        
        // 4. Median Acceptance Rate (per-user)
        var userAcceptanceRates = filtered
            .GroupBy(r => r.UserLogin)
            .Select(g => {
                var generations = g.Sum(r => r.CodeGenerationActivityCount);
                var acceptances = g.Sum(r => r.CodeAcceptanceActivityCount);
                return generations > 0 ? (double)acceptances / generations : 0.0;
            })
            .OrderBy(x => x)
            .ToList();
        
        var medianAcceptanceRate = 0.0;
        if (userAcceptanceRates.Any())
        {
            var mid = userAcceptanceRates.Count / 2;
            medianAcceptanceRate = userAcceptanceRates.Count % 2 == 0
                ? (userAcceptanceRates[mid - 1] + userAcceptanceRates[mid]) / 2.0
                : userAcceptanceRates[mid];
        }
        
        // 5. Acceptances per Active User per Working Day
        var totalAcceptances = filtered.Sum(r => r.CodeAcceptanceActivityCount);
        var acceptancesPerActiveUserPerDay = (workingDays > 0 && activeUserCount > 0) 
            ? (double)totalAcceptances / (activeUserCount * workingDays) : 0;
        
        // 6. Power Users % (users meeting both acceptance rate and active days thresholds)
        var powerUserCount = 0;
        var userStats = filtered
            .GroupBy(r => r.UserLogin)
            .Select(g => new {
                User = g.Key,
                AcceptanceRate = g.Sum(r => r.CodeGenerationActivityCount) > 0 
                    ? (double)g.Sum(r => r.CodeAcceptanceActivityCount) / g.Sum(r => r.CodeGenerationActivityCount) : 0,
                ActiveDays = g.Select(r => r.Day).Distinct().Count()
            })
            .ToList();
        
        powerUserCount = userStats.Count(u => u.AcceptanceRate >= PowerUserAcceptanceThreshold 
            && u.ActiveDays >= PowerUserActiveDaysThreshold);
        var powerUsersPercent = activeUserCount > 0 ? (double)powerUserCount / activeUserCount : 0;
        
        // 7. Inline Share % (code_completion generations vs total)
        var inlineGenerations = filtered
            .SelectMany(r => r.TotalsByFeature)
            .Where(f => f.Feature.Equals("code_completion", StringComparison.OrdinalIgnoreCase))
            .Sum(f => f.CodeGenerationActivityCount);
        var totalGenerations = filtered.Sum(r => r.CodeGenerationActivityCount);
        var inlineSharePercent = totalGenerations > 0 ? (double)inlineGenerations / totalGenerations : 0;
        
        // 8. Chat Adoption % (users with any chat usage vs active users)
        var chatUsers = filtered.Where(r => r.UsedChat).Select(r => r.UserLogin).Distinct().Count();
        var chatAdoptionPercent = activeUserCount > 0 ? (double)chatUsers / activeUserCount : 0;
        
        // 9. Model Leader Margin (best model vs overall acceptance rate)
        var overallAcceptanceRate = totalGenerations > 0 ? (double)totalAcceptances / totalGenerations : 0;
        var modelAcceptanceRates = filtered
            .SelectMany(r => r.TotalsByModelFeature)
            .GroupBy(m => m.Model)
            .Select(g => new {
                Model = g.Key,
                AcceptanceRate = g.Sum(m => m.CodeGenerationActivityCount) > 0 
                    ? (double)g.Sum(m => m.CodeAcceptanceActivityCount) / g.Sum(m => m.CodeGenerationActivityCount) : 0
            })
            .ToList();
        
        var bestModelRate = modelAcceptanceRates.Any() ? modelAcceptanceRates.Max(m => m.AcceptanceRate) : 0;
        var modelLeaderMargin = bestModelRate - overallAcceptanceRate;
        
        // 10. Concentration Index (Gini coefficient for generation distribution)
        var userGenerations = filtered
            .GroupBy(r => r.UserLogin)
            .Select(g => g.Sum(r => r.CodeGenerationActivityCount))
            .OrderBy(x => x)
            .ToArray();
        
        var concentrationIndex = CalculateGiniCoefficient(userGenerations);
        
        // 11. Ramp Rate (users/week) - calculate from last 4 weeks if possible
        var rampRateUsersPerWeek = CalculateRampRate(filtered);
        
        // 12. Time-to-First-Value (median days from first seen to first acceptance)
        var timeToFirstValueDays = CalculateTimeToFirstValue(filtered);
        
        // 13. Language Coverage % (users with activity in top language vs active users)
        var languageCoveragePercent = CalculateLanguageCoverage(filtered, activeUserCount);
        
        // 14. Estimated Time Saved (acceptances × configurable seconds per acceptance)
        var estimatedTimeSavedHours = (totalAcceptances * SecondsPerAcceptance) / 3600.0; // Convert to hours
        
        return new EngineeringMetrics(
            licenseUtilization,
            unusedSeats,
            engagedUsersPercent,
            usageRate,
            medianAcceptanceRate,
            acceptancesPerActiveUserPerDay,
            powerUsersPercent,
            inlineSharePercent,
            chatAdoptionPercent,
            modelLeaderMargin,
            concentrationIndex,
            rampRateUsersPerWeek,
            timeToFirstValueDays,
            languageCoveragePercent,
            estimatedTimeSavedHours
        );
    }
    
    private double CalculateGiniCoefficient(int[] values)
    {
        if (values.Length <= 1) return 0.0;
        
        var n = values.Length;
        var sum = values.Sum();
        if (sum == 0) return 0.0;
        
        var gini = 0.0;
        for (int i = 0; i < n; i++)
        {
            gini += (2 * (i + 1) - n - 1) * values[i];
        }
        
        return gini / (n * sum);
    }
    
    private double CalculateRampRate(List<CopilotRecord> filtered)
    {
        if (!filtered.Any()) return 0.0;
        
        // Get weekly active user counts for trend analysis
        var weeklyUsers = filtered
            .GroupBy(r => new { 
                Year = r.Day.Year, 
                Week = System.Globalization.ISOWeek.GetWeekOfYear(r.Day.ToDateTime(TimeOnly.MinValue))
            })
            .Select(g => new {
                WeekKey = g.Key,
                ActiveUsers = g.Select(r => r.UserLogin).Distinct().Count()
            })
            .OrderBy(w => w.WeekKey.Year).ThenBy(w => w.WeekKey.Week)
            .ToList();
        
        if (weeklyUsers.Count < 2) return 0.0;
        
        // Simple linear regression to get slope (users per week)
        var n = weeklyUsers.Count;
        var sumX = n * (n + 1) / 2.0; // 1 + 2 + ... + n
        var sumY = weeklyUsers.Sum(w => w.ActiveUsers);
        var sumXY = weeklyUsers.Select((w, i) => (i + 1) * w.ActiveUsers).Sum();
        var sumX2 = n * (n + 1) * (2 * n + 1) / 6.0; // 1² + 2² + ... + n²
        
        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }
    
    private double CalculateTimeToFirstValue(List<CopilotRecord> filtered)
    {
        var userFirstAcceptance = filtered
            .Where(r => r.CodeAcceptanceActivityCount > 0)
            .GroupBy(r => r.UserLogin)
            .Select(g => g.Min(r => r.Day))
            .ToList();
        
        var userFirstSeen = filtered
            .GroupBy(r => r.UserLogin)
            .Select(g => g.Min(r => r.Day))
            .ToList();
        
        var timeToValueDays = new List<double>();
        foreach (var user in filtered.Select(r => r.UserLogin).Distinct())
        {
            var firstSeen = filtered.Where(r => r.UserLogin == user).Min(r => r.Day);
            var firstAcceptance = filtered
                .Where(r => r.UserLogin == user && r.CodeAcceptanceActivityCount > 0)
                .Select(r => r.Day)
                .FirstOrDefault();
            
            if (firstAcceptance != default)
            {
                timeToValueDays.Add((firstAcceptance.ToDateTime(TimeOnly.MinValue) - 
                                   firstSeen.ToDateTime(TimeOnly.MinValue)).TotalDays);
            }
        }
        
        if (!timeToValueDays.Any()) return 0.0;
        
        timeToValueDays.Sort();
        var mid = timeToValueDays.Count / 2;
        return timeToValueDays.Count % 2 == 0
            ? (timeToValueDays[mid - 1] + timeToValueDays[mid]) / 2.0
            : timeToValueDays[mid];
    }
    
    private double CalculateLanguageCoverage(List<CopilotRecord> filtered, int activeUserCount)
    {
        if (activeUserCount == 0) return 0.0;
        
        // Find the most popular language
        var languageUsage = filtered
            .SelectMany(r => r.TotalsByLanguageModel)
            .GroupBy(l => l.Language)
            .Select(g => new { 
                Language = g.Key, 
                Users = filtered.Where(r => r.TotalsByLanguageModel.Any(lm => lm.Language == g.Key))
                                .Select(r => r.UserLogin).Distinct().Count()
            })
            .OrderByDescending(l => l.Users)
            .FirstOrDefault();
        
        if (languageUsage == null) return 0.0;
        
        return (double)languageUsage.Users / activeUserCount;
    }
}

internal sealed class DateOnlyJsonConverter : System.Text.Json.Serialization.JsonConverter<DateOnly>
{
    private const string Primary = "yyyy-MM-dd";
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (!string.IsNullOrEmpty(s) && DateOnly.TryParseExact(s, Primary, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            if (DateOnly.TryParse(s, out var d2)) return d2;
        }
        throw new JsonException("Invalid DateOnly value");
    }
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Primary, CultureInfo.InvariantCulture));
    }
}
