# Copilot Stats Viewer

A comprehensive analytics dashboard for GitHub Copilot usage data, providing insights into AI development enablement across your organization.

## Overview

The Copilot Stats Viewer is a Blazor Server application that analyzes GitHub Copilot usage data to provide meaningful metrics and visualizations. It calculates the **AIDEI (AI Development Enablement Index)** - a composite score that measures how effectively your organization is adopting and utilizing AI-powered development tools.

## Features

- 📊 **Interactive Dashboards** - Real-time charts and visualizations
- 👥 **User Analytics** - Individual and team performance metrics  
- 🎯 **AIDEI Score** - Comprehensive AI enablement assessment
- 🔍 **Advanced Filtering** - Filter by date range, users, features, and models
- 📈 **Time Series Analysis** - Track adoption trends over time
- 📋 **Detailed Data Tables** - Drill down into individual user activity
- 📱 **Responsive Design** - Works on desktop and mobile devices

## Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd copilot-stats
   ```

2. **Run the application**
   ```bash
   dotnet run --project copiloty-stats-viewer
   ```

3. **Load your data**
   - Click "Select File" to upload your Copilot usage data (JSON/NDJSON format)
   - Or click "Load Sample Data" to try with example data
   - Enter your total licensed users count for accurate adoption rates

4. **Explore the metrics**
   - View AIDEI scores and breakdown
   - Analyze user adoption patterns
   - Track usage trends over time

## Data Requirements

The application expects GitHub Copilot usage data in NDJSON (Newline Delimited JSON) format. Each record should contain:

- **User Information**: `user_login`, `user_id`
- **Activity Counts**: `user_initiated_interaction_count`, `code_generation_activity_count`, `code_acceptance_activity_count`
- **Date Information**: `day`, `report_start_day`, `report_end_day`
- **Feature Breakdowns**: `totals_by_feature`, `totals_by_model_feature`, `totals_by_language_model`
- **Usage Flags**: `used_chat`, `used_agent`

## AIDEI: AI Development Enablement Index

The AIDEI score provides a comprehensive measure of how effectively your organization is enabling AI-powered development. It combines four key metrics:

### Formula
```
AIDEI = (Adoption Rate × 40%) + (Acceptance Rate × 40%) + (Licensed vs Engaged Rate × 20%)
```

### Key Metrics Explained

#### 1. **Adoption Rate** (40% weight)
- **What it measures**: Percentage of licensed users who actively use Copilot
- **Calculation**: `Active Users ÷ Total Licensed Users`
- **Business meaning**: Shows how many of your Copilot licenses are actually being utilized
- **Example**: If you have 500 licenses and 150 users are active → 30% adoption rate

#### 2. **Acceptance Rate** (40% weight)  
- **What it measures**: Percentage of AI suggestions that developers accept
- **Calculation**: `Accepted Suggestions ÷ Generated Suggestions`
- **Business meaning**: Indicates the quality and relevance of AI suggestions to your codebase
- **Example**: 1,000 suggestions generated, 700 accepted → 70% acceptance rate

#### 3. **Licensed vs Engaged Rate** (20% weight)
- **What it measures**: Percentage of licensed users who have meaningful daily engagement
- **Calculation**: `Users With Meaningful Engagement ÷ Total Licensed Users`
- **Meaningful Engagement**: Users who have >3 activities per day on at least 2 days
- **Business meaning**: Shows how many licenses result in productive, sustained usage rather than just experimentation
- **Example**: 500 licensed users, 80 with meaningful engagement → 16% engaged rate

### AIDEI Score Interpretation

| Grade | Score Range | Interpretation |
|-------|-------------|----------------|
| A+ | 90-100% | Exceptional AI enablement - best in class adoption and effectiveness |
| A | 80-89% | Excellent AI integration with high adoption and acceptance |
| B+ | 70-79% | Good AI utilization with room for improvement |
| B | 60-69% | Moderate adoption - focus on user training and engagement |
| C+ | 50-59% | Basic usage - significant opportunity for improvement |
| C | 40-49% | Limited adoption - review training and change management |
| D | 30-39% | Poor utilization - major intervention needed |
| F | 0-29% | Minimal adoption - fundamental issues to address |

## Business Logic Deep Dive

### Data Processing Rules

#### User Activity Classification
- **Active User**: Has any interactions OR code generations
- **Meaningful Daily Usage**: >3 combined interactions and generations in a single day
- **Working Days**: Monday through Friday (weekends excluded from calculations)

#### Filtering Logic
The application applies filters in this order:
1. **Date Range**: Include only records within specified dates
2. **User Selection**: Include only selected users (if any specified)  
3. **Feature Filter**: Include only records using specified features
4. **Model Filter**: Include only records using specified AI models

#### Time Series Calculations
- Groups data by day
- Sums all activity within each day across users
- Provides trends for interactions, generations, and acceptances over time

#### User Performance Ranking
- Ranks users by total code generations (primary metric)
- Secondary ranking by acceptances
- Shows top 10 users by default

### Key Business Insights

#### Adoption Patterns
- **High Adoption, Low Acceptance**: Users trying Copilot but finding suggestions irrelevant
- **Low Adoption, High Acceptance**: Small group of power users - opportunity to expand
- **High Both**: Excellent - Copilot is valuable and well-adopted
- **Low Both**: Fundamental issues - training, technical problems, or cultural resistance

#### Usage Consistency  
- **Daily Usage Rate >70%**: Copilot is integral to development workflow
- **Daily Usage Rate 40-70%**: Moderate integration - some workflow adoption
- **Daily Usage Rate <40%**: Sporadic usage - not yet part of regular practice

#### Feature Analysis
- **Chat vs Completion Usage**: Shows preference for interactive vs inline assistance
- **Model Performance**: Identifies which AI models work best for your codebase
- **Language Patterns**: Reveals which programming languages see highest AI adoption

## Architecture

### Technology Stack
- **Backend**: ASP.NET Core 9, Blazor Server
- **Frontend**: Bootstrap 5, Chart.js for visualizations
- **Data Format**: NDJSON (Newline Delimited JSON)
- **Hosting**: Self-hosted web application

### Key Components
- **DataService**: Core business logic and calculations
- **Home**: Main dashboard and data loading
- **Charts**: Interactive visualizations using Chart.js
- **DataTable**: Detailed user activity with grouping and sorting
- **Filters**: Date range, user, feature, and model filtering

## Customization

### Adding New Metrics
1. Extend `AIDEIMetrics` record in `DataService.cs`
2. Add calculation logic in `GetAIDEI()` method
3. Update dashboard display in `DataTable.razor`

### Modifying AIDEI Weights
Adjust the weights in the AIDEI calculation:
```csharp
var aideiScore = (adoptionRate * 0.4) + (acceptanceRate * 0.4) + (licensedVsEngagedRate * 0.2);
```

### Custom Filters
Add new filter properties to `DataService` and update `GetFiltered()` method.

## Troubleshooting

### Common Issues

**Charts not loading**: Ensure Chart.js is loaded and interactive render mode is enabled

**High memory usage**: For very large datasets (>100MB), consider implementing streaming or pagination

**Slow performance**: Check if you're loading too much data - use date filters to limit scope

**Inaccurate adoption rates**: Ensure you've entered the correct total licensed users count

### Data Validation
The application skips invalid JSON lines automatically. Check console output for parsing errors if data seems incomplete.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable  
5. Submit a pull request

## License

View the [LICENSE](LICENSE) file for details.

## Support

For questions or issues, please open an issue on GitHub or contact the maintainer.
