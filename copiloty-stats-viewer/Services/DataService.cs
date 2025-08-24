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
            .GroupBy(m => m.Model)
            .Select(g => (Model: g.Key, Generations: g.Sum(m => m.CodeGenerationActivityCount)))
            .OrderByDescending(t => t.Generations);
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

    public sealed record Totals(int Interactions, int Generations, int Acceptances)
    {
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

    // AIDEI (AI Development Enablement Index) calculations
    public sealed record AIDEIMetrics(
        double AdoptionRate,
        double AcceptanceRate, 
        double LicensedVsEngagedRate,
        double UsageRate,
        double AIDEIScore
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
            // Get users who have meaningful engagement (>3 activities on at least 2 days)
            var meaningfullyEngagedUsers = filtered.GroupBy(r => r.UserLogin)
                .Where(g => g.Count(r => (r.UserInitiatedInteractionCount + r.CodeGenerationActivityCount) > 3) >= 2)
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
