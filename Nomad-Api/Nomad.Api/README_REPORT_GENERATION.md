# Report Generation System

## Overview

This system generates performance reports using a **fixed HTML/CSS template** with dynamic placeholder replacement. The layout, charts, graphs, and styling remain consistent, while only content and brand colors change per report.

## Architecture

### Components

1. **HTML Template** (`Templates/ReportTemplate.html`)
   - Fixed design and layout
   - Contains placeholders like `{{COMPANY_NAME}}`, `{{SUBJECT_NAME}}`, etc.
   - CSS variables for brand colors that are replaced dynamically

2. **ReportTemplateService** (`Services/ReportTemplateService.cs`)
   - Loads HTML template from file system
   - Replaces placeholders with actual data from survey APIs
   - Applies brand colors dynamically
   - Generates charts as images
   - Converts HTML to PDF

3. **ReportController** (`Controllers/ReportController.cs`)
   - API endpoints for generating HTML and PDF reports
   - Accepts brand colors, logo URLs, and report parameters

## Placeholders

### Available Placeholders

#### Company Information
- `{{COMPANY_NAME}}` - Company/organization name
- `{{COMPANY_LOGO}}` - Company logo (replaced with `<img>` tag)

#### Subject Information
- `{{SUBJECT_NAME}}` - Person being evaluated
- `{{SUBJECT_DEPARTMENT}}` - Department
- `{{SUBJECT_POSITION}}` - Job position
- `{{SUBJECT_EMPLOYEE_ID}}` - Employee ID

#### Dates
- `{{REPORT_DATE}}` - Current date (formatted)
- `{{REPORT_PERIOD}}` - Report period (month/year)
- `{{GENERATION_DATE}}` - Report generation timestamp

#### Scores (with automatic rating mapping)
- `{{OVERALL_SCORE}}` - Overall performance score
- `{{OVERALL_RATING}}` - Rating (Excellent, Good, etc.)
- `{{SELF_SCORE}}` - Self-evaluation score
- `{{SELF_RATING}}` - Self-evaluation rating
- `{{EVALUATOR_SCORE}}` - Evaluator average score
- `{{EVALUATOR_RATING}}` - Evaluator rating
- `{{SELF_AVERAGE_SCORE}}` - Self-evaluation average
- `{{EVALUATOR_AVERAGE_SCORE}}` - Evaluator average

#### Performance Indicators
- `{{SATISFACTION_SCORE}}` - Satisfaction score
- `{{IMPROVEMENT_SCORE}}` - Improvement score
- `{{ORGANIZATION_COMPARISON}}` - Comparison with organization average

#### Charts (replaced with image tags)
- `{{CHART_OVERALL_SATISFACTION}}` - Overall satisfaction chart
- `{{CHART_QUESTION_PERFORMANCE}}` - Question-wise performance chart
- `{{CHART_SELF_VS_EVALUATOR}}` - Self vs evaluator comparison chart

#### Tables
- `{{QUESTIONS_TABLE}}` - Full HTML table with question scores

#### Brand Colors (CSS Variables)
- `{{PRIMARY_COLOR}}` - Primary brand color (default: #0455A4)
- `{{SECONDARY_COLOR}}` - Secondary brand color (default: #1D8F6C)
- `{{TERTIARY_COLOR}}` - Tertiary brand color (default: #6C757D)

## Usage

### Generate HTML Report

```http
POST /{tenantSlug}/api/reports/generate/html
Content-Type: application/json

{
  "subjectId": "guid",
  "surveyId": "guid (optional)",
  "companyLogoUrl": "https://cloudinary.com/... (optional)",
  "primaryColor": "#0455A4 (optional)",
  "secondaryColor": "#1D8F6C (optional)",
  "tertiaryColor": "#6C757D (optional)"
}
```

### Generate PDF Report

```http
POST /{tenantSlug}/api/reports/generate/pdf
Content-Type: application/json

{
  "subjectId": "guid",
  "surveyId": "guid (optional)",
  "companyLogoUrl": "https://cloudinary.com/... (optional)",
  "primaryColor": "#0455A4 (optional)",
  "secondaryColor": "#1D8F6C (optional)",
  "tertiaryColor": "#6C757D (optional)"
}
```

## Best Practices

### 1. Naming Placeholders

- Use `UPPER_CASE` with `_` separators
- Use descriptive names: `{{COMPANY_NAME}}` not `{{COMP}}`
- Group related placeholders: `{{SUBJECT_NAME}}`, `{{SUBJECT_DEPARTMENT}}`

### 2. Positioning Images/Logos

- Logo placeholder should be in a flex container for proper alignment
- Use `max-width` and `max-height` in CSS to control size
- Always provide alt text: `<img src="..." alt="Company Logo" class="logo" />`

### 3. Handling Missing Data

- The service uses null coalescing: `value ?? "N/A"`
- Optional fields should have defaults in template
- Use conditional rendering for optional sections

### 4. Rating Mapping

Scores are automatically mapped to ratings:
- **90-100**: "Excellent"
- **80-89**: "Very Good"
- **70-79**: "Good"
- **60-69**: "Average"
- **50-59**: "Below Average"
- **<50**: "Needs Improvement"

### 5. Caching Brand Assets

- Company logos: Cache Cloudinary URLs in tenant settings
- Brand colors: Store in tenant configuration table
- Chart generation: Cache chart images based on data hash

### 6. Chart Generation

Charts are generated as PNG images using SkiaSharp:
- Default size: 800x600 pixels
- Format: PNG with 100% quality
- Embedded as base64 or served as URLs

### 7. HTML Escaping

- All user-provided text is HTML-escaped to prevent XSS
- Use `EscapeHtml()` method for all dynamic content
- Images URLs should be validated before insertion

### 8. Performance

- Template is loaded once and cached in memory
- Chart generation is done in parallel when possible
- PDF generation uses QuestPDF for efficient rendering

## Customization

### Adding New Placeholders

1. Add placeholder to HTML template: `{{NEW_PLACEHOLDER}}`
2. Add replacement logic in `ReplacePlaceholders()` method
3. Update this documentation

### Modifying Template Design

1. Edit `Templates/ReportTemplate.html`
2. Maintain placeholder names (don't rename existing ones)
3. Test with various data scenarios
4. Ensure responsive design for A4 PDF output

### Adding New Charts

1. Create chart generation method in `ReportTemplateService`
2. Add placeholder in HTML: `{{CHART_NEW_CHART}}`
3. Replace placeholder with `<img>` tag containing base64 or URL
4. Update service registration if needed

## Example Flow

```
1. User requests report for Subject ID
2. ReportTemplateService loads HTML template
3. Fetches comprehensive report data from ReportingService
4. Replaces all placeholders with actual data
5. Generates charts as images (SkiaSharp)
6. Embeds chart images in HTML
7. Applies brand colors from tenant settings or request
8. Returns HTML or converts to PDF (QuestPDF)
9. PDF is returned as download
```

## Error Handling

- Missing data: Returns "N/A" for optional fields
- Invalid Subject ID: Returns 404 with error message
- Template not found: Returns 500 error
- Chart generation failure: Uses placeholder text

## Future Enhancements

- [ ] Support for multiple templates per tenant
- [ ] Chart generation using dedicated charting library
- [ ] Base64 image embedding for offline PDFs
- [ ] Template versioning
- [ ] Preview mode before generation
- [ ] Batch report generation
- [ ] Email delivery option


