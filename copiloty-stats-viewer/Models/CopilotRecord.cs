using System.Text.Json.Serialization;

namespace copiloty_stats_viewer.Models;

public sealed class CopilotRecord
{
    [JsonPropertyName("report_start_day")] public DateOnly ReportStartDay { get; set; }
    [JsonPropertyName("report_end_day")] public DateOnly ReportEndDay { get; set; }
    [JsonPropertyName("day")] public DateOnly Day { get; set; }

    [JsonPropertyName("enterprise_id")] public string EnterpriseId { get; set; } = string.Empty;
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("user_login")] public string UserLogin { get; set; } = string.Empty;

    [JsonPropertyName("user_initiated_interaction_count")] public int UserInitiatedInteractionCount { get; set; }
    [JsonPropertyName("code_generation_activity_count")] public int CodeGenerationActivityCount { get; set; }
    [JsonPropertyName("code_acceptance_activity_count")] public int CodeAcceptanceActivityCount { get; set; }
    [JsonPropertyName("generated_loc_sum")] public int GeneratedLocSum { get; set; }
    [JsonPropertyName("accepted_loc_sum")] public int AcceptedLocSum { get; set; }

    [JsonPropertyName("totals_by_ide")] public List<IdeTotals> TotalsByIde { get; set; } = new();
    [JsonPropertyName("totals_by_feature")] public List<FeatureTotals> TotalsByFeature { get; set; } = new();
    [JsonPropertyName("totals_by_language_feature")] public List<LanguageFeatureTotals> TotalsByLanguageFeature { get; set; } = new();
    [JsonPropertyName("totals_by_language_model")] public List<LanguageModelTotals> TotalsByLanguageModel { get; set; } = new();
    [JsonPropertyName("totals_by_model_feature")] public List<ModelFeatureTotals> TotalsByModelFeature { get; set; } = new();

    [JsonPropertyName("used_agent")] public bool UsedAgent { get; set; }
    [JsonPropertyName("used_chat")] public bool UsedChat { get; set; }
}

public sealed class IdeTotals
{
    [JsonPropertyName("ide")] public string Ide { get; set; } = string.Empty;
    [JsonPropertyName("user_initiated_interaction_count")] public int UserInitiatedInteractionCount { get; set; }
    [JsonPropertyName("code_generation_activity_count")] public int CodeGenerationActivityCount { get; set; }
    [JsonPropertyName("code_acceptance_activity_count")] public int CodeAcceptanceActivityCount { get; set; }
    [JsonPropertyName("generated_loc_sum")] public int GeneratedLocSum { get; set; }
    [JsonPropertyName("accepted_loc_sum")] public int AcceptedLocSum { get; set; }

    [JsonPropertyName("last_known_plugin_version")] public PluginVersion? LastKnownPluginVersion { get; set; }
}

public sealed class PluginVersion
{
    [JsonPropertyName("sampled_at")] public DateTime SampledAt { get; set; }
    [JsonPropertyName("plugin")] public string Plugin { get; set; } = string.Empty;
    [JsonPropertyName("plugin_version")] public string PluginVersionString { get; set; } = string.Empty;
}

public sealed class FeatureTotals
{
    [JsonPropertyName("feature")] public string Feature { get; set; } = string.Empty;
    [JsonPropertyName("user_initiated_interaction_count")] public int UserInitiatedInteractionCount { get; set; }
    [JsonPropertyName("code_generation_activity_count")] public int CodeGenerationActivityCount { get; set; }
    [JsonPropertyName("code_acceptance_activity_count")] public int CodeAcceptanceActivityCount { get; set; }
    [JsonPropertyName("generated_loc_sum")] public int GeneratedLocSum { get; set; }
    [JsonPropertyName("accepted_loc_sum")] public int AcceptedLocSum { get; set; }
}

public sealed class LanguageFeatureTotals
{
    [JsonPropertyName("language")] public string Language { get; set; } = string.Empty;
    [JsonPropertyName("feature")] public string Feature { get; set; } = string.Empty;
    [JsonPropertyName("code_generation_activity_count")] public int CodeGenerationActivityCount { get; set; }
    [JsonPropertyName("code_acceptance_activity_count")] public int CodeAcceptanceActivityCount { get; set; }
    [JsonPropertyName("generated_loc_sum")] public int GeneratedLocSum { get; set; }
    [JsonPropertyName("accepted_loc_sum")] public int AcceptedLocSum { get; set; }
}

public sealed class LanguageModelTotals
{
    [JsonPropertyName("language")] public string Language { get; set; } = string.Empty;
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("code_generation_activity_count")] public int CodeGenerationActivityCount { get; set; }
    [JsonPropertyName("code_acceptance_activity_count")] public int CodeAcceptanceActivityCount { get; set; }
    [JsonPropertyName("generated_loc_sum")] public int GeneratedLocSum { get; set; }
    [JsonPropertyName("accepted_loc_sum")] public int AcceptedLocSum { get; set; }
}

public sealed class ModelFeatureTotals
{
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("feature")] public string Feature { get; set; } = string.Empty;
    [JsonPropertyName("user_initiated_interaction_count")] public int UserInitiatedInteractionCount { get; set; }
    [JsonPropertyName("code_generation_activity_count")] public int CodeGenerationActivityCount { get; set; }
    [JsonPropertyName("code_acceptance_activity_count")] public int CodeAcceptanceActivityCount { get; set; }
    [JsonPropertyName("generated_loc_sum")] public int GeneratedLocSum { get; set; }
    [JsonPropertyName("accepted_loc_sum")] public int AcceptedLocSum { get; set; }
}
