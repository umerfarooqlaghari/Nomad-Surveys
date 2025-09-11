'use client';

import React, { useState } from 'react';
import styles from './ReportsTab.module.css';

export default function ReportsTab() {
  const [selectedReport, setSelectedReport] = useState('overview');
  const [dateRange, setDateRange] = useState('last30days');

  // Mock reports data
  const reportTypes = [
    { id: 'overview', name: 'Overview Report', description: 'General survey analytics and insights' },
    { id: 'completion', name: 'Completion Report', description: 'Survey completion rates and trends' },
    { id: 'response', name: 'Response Analysis', description: 'Detailed response analysis and patterns' },
    { id: 'tenant', name: 'Tenant Performance', description: 'Performance metrics by tenant/company' },
    { id: 'user', name: 'User Engagement', description: 'User participation and engagement metrics' },
  ];

  const surveyResults = [
    {
      id: 1,
      surveyTitle: 'Employee Satisfaction Q4 2024',
      tenant: 'Acme Corporation',
      totalResponses: 156,
      completionRate: 78.5,
      avgScore: 4.2,
      status: 'Completed',
      dateCompleted: '2024-01-15',
    },
    {
      id: 2,
      surveyTitle: 'Leadership Assessment 2024',
      tenant: 'TechCorp Inc.',
      totalResponses: 89,
      completionRate: 65.2,
      avgScore: 3.8,
      status: 'Active',
      dateCompleted: null,
    },
    {
      id: 3,
      surveyTitle: 'Customer Feedback Q1',
      tenant: 'Global Solutions',
      totalResponses: 234,
      completionRate: 92.1,
      avgScore: 4.6,
      status: 'Completed',
      dateCompleted: '2024-02-01',
    },
  ];

  const analyticsData = {
    totalSurveys: 45,
    totalResponses: 2847,
    avgCompletionRate: 73.2,
    avgSatisfactionScore: 4.1,
    topPerformingTenant: 'Global Solutions',
    mostEngagedUsers: 1247,
  };

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div>
          <h2 className={styles.title}>Reports & Analytics</h2>
          <p className={styles.subtitle}>Survey results, analytics, and performance insights</p>
        </div>
        <div className={styles.controls}>
          <select 
            value={dateRange} 
            onChange={(e) => setDateRange(e.target.value)}
            className={styles.select}
          >
            <option value="last7days">Last 7 Days</option>
            <option value="last30days">Last 30 Days</option>
            <option value="last90days">Last 90 Days</option>
            <option value="lastyear">Last Year</option>
            <option value="custom">Custom Range</option>
          </select>
          <button className={styles.exportButton}>
            Export Report
          </button>
        </div>
      </div>

      {/* Report Type Selection */}
      <div className={styles.reportTypes}>
        <h3 className={styles.sectionTitle}>Report Types</h3>
        <div className={styles.reportGrid}>
          {reportTypes.map((report) => (
            <div 
              key={report.id} 
              className={`${styles.reportCard} ${selectedReport === report.id ? styles.reportCardActive : ''}`}
              onClick={() => setSelectedReport(report.id)}
            >
              <h4 className={styles.reportCardTitle}>{report.name}</h4>
              <p className={styles.reportCardDescription}>{report.description}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Analytics Overview */}
      <div className={styles.analyticsSection}>
        <h3 className={styles.sectionTitle}>Key Metrics</h3>
        <div className={styles.metricsGrid}>
          <div className={styles.metricCard}>
            <div className={styles.metricIcon}>SURV</div>
            <div className={styles.metricDetails}>
              <div className={styles.metricValue}>{analyticsData.totalSurveys}</div>
              <div className={styles.metricLabel}>Total Surveys</div>
            </div>
          </div>
          <div className={styles.metricCard}>
            <div className={styles.metricIcon}>RESP</div>
            <div className={styles.metricDetails}>
              <div className={styles.metricValue}>{analyticsData.totalResponses.toLocaleString()}</div>
              <div className={styles.metricLabel}>Total Responses</div>
            </div>
          </div>
          <div className={styles.metricCard}>
            <div className={styles.metricIcon}>RATE</div>
            <div className={styles.metricDetails}>
              <div className={styles.metricValue}>{analyticsData.avgCompletionRate}%</div>
              <div className={styles.metricLabel}>Avg Completion Rate</div>
            </div>
          </div>
          <div className={styles.metricCard}>
            <div className={styles.metricIcon}>SCORE</div>
            <div className={styles.metricDetails}>
              <div className={styles.metricValue}>{analyticsData.avgSatisfactionScore}/5</div>
              <div className={styles.metricLabel}>Avg Satisfaction</div>
            </div>
          </div>
        </div>
      </div>

      {/* Charts Placeholder */}
      <div className={styles.chartsSection}>
        <h3 className={styles.sectionTitle}>Analytics Charts</h3>
        <div className={styles.chartsGrid}>
          <div className={styles.chartCard}>
            <h4 className={styles.chartTitle}>Response Trends</h4>
            <div className={styles.chartPlaceholder}>
              <div className={styles.chartPlaceholderContent}>
                <div className={styles.chartPlaceholderIcon}>CHART</div>
                <div className={styles.chartPlaceholderText}>Response trends over time</div>
                <div className={styles.chartPlaceholderSubtext}>Chart integration coming soon</div>
              </div>
            </div>
          </div>
          <div className={styles.chartCard}>
            <h4 className={styles.chartTitle}>Completion Rates by Tenant</h4>
            <div className={styles.chartPlaceholder}>
              <div className={styles.chartPlaceholderContent}>
                <div className={styles.chartPlaceholderIcon}>CHART</div>
                <div className={styles.chartPlaceholderText}>Tenant performance comparison</div>
                <div className={styles.chartPlaceholderSubtext}>Chart integration coming soon</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Survey Results Table */}
      <div className={styles.resultsSection}>
        <h3 className={styles.sectionTitle}>Survey Results</h3>
        <div className={styles.tableContainer}>
          <table className={styles.table}>
            <thead className={styles.tableHead}>
              <tr>
                <th className={styles.tableHeader}>Survey</th>
                <th className={styles.tableHeader}>Tenant</th>
                <th className={styles.tableHeader}>Responses</th>
                <th className={styles.tableHeader}>Completion Rate</th>
                <th className={styles.tableHeader}>Avg Score</th>
                <th className={styles.tableHeader}>Status</th>
                <th className={styles.tableHeader}>Date</th>
                <th className={styles.tableHeader}>Actions</th>
              </tr>
            </thead>
            <tbody className={styles.tableBody}>
              {surveyResults.map((result) => (
                <tr key={result.id} className={styles.tableRow}>
                  <td className={styles.tableCell}>
                    <div className={styles.surveyInfo}>
                      <div className={styles.surveyTitle}>{result.surveyTitle}</div>
                    </div>
                  </td>
                  <td className={styles.tableCell}>{result.tenant}</td>
                  <td className={styles.tableCell}>
                    <span className={styles.responseBadge}>{result.totalResponses}</span>
                  </td>
                  <td className={styles.tableCell}>
                    <div className={styles.progressContainer}>
                      <div className={styles.progressBar}>
                        <div 
                          className={styles.progressFill} 
                          style={{ width: `${result.completionRate}%` }}
                        ></div>
                      </div>
                      <span className={styles.progressText}>{result.completionRate}%</span>
                    </div>
                  </td>
                  <td className={styles.tableCell}>
                    <span className={styles.scoreBadge}>{result.avgScore}/5</span>
                  </td>
                  <td className={styles.tableCell}>
                    <span className={`${styles.statusBadge} ${
                      result.status === 'Completed' ? styles.statusCompleted : styles.statusActive
                    }`}>
                      {result.status}
                    </span>
                  </td>
                  <td className={styles.tableCell}>
                    {result.dateCompleted || 'In Progress'}
                  </td>
                  <td className={styles.tableCell}>
                    <div className={styles.actionButtons}>
                      <button className={styles.actionButton}>View</button>
                      <button className={styles.actionButton}>Export</button>
                      <button className={styles.actionButton}>Share</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Insights Section */}
      <div className={styles.insightsSection}>
        <h3 className={styles.sectionTitle}>Key Insights</h3>
        <div className={styles.insightsGrid}>
          <div className={styles.insightCard}>
            <h4 className={styles.insightTitle}>Top Performing Tenant</h4>
            <p className={styles.insightValue}>{analyticsData.topPerformingTenant}</p>
            <p className={styles.insightDescription}>Highest completion rate and satisfaction scores</p>
          </div>
          <div className={styles.insightCard}>
            <h4 className={styles.insightTitle}>Most Engaged Users</h4>
            <p className={styles.insightValue}>{analyticsData.mostEngagedUsers.toLocaleString()}</p>
            <p className={styles.insightDescription}>Active participants across all surveys</p>
          </div>
          <div className={styles.insightCard}>
            <h4 className={styles.insightTitle}>Improvement Opportunity</h4>
            <p className={styles.insightValue}>Response Time</p>
            <p className={styles.insightDescription}>Average response time can be reduced by 15%</p>
          </div>
        </div>
      </div>

      {/* Export Options */}
      <div className={styles.exportSection}>
        <h3 className={styles.sectionTitle}>Export Options</h3>
        <div className={styles.exportGrid}>
          <button className={styles.exportOption}>
            <div className={styles.exportIcon}>PDF</div>
            <div className={styles.exportText}>
              <div className={styles.exportTitle}>PDF Report</div>
              <div className={styles.exportDescription}>Comprehensive report with charts</div>
            </div>
          </button>
          <button className={styles.exportOption}>
            <div className={styles.exportIcon}>CSV</div>
            <div className={styles.exportText}>
              <div className={styles.exportTitle}>CSV Data</div>
              <div className={styles.exportDescription}>Raw data for analysis</div>
            </div>
          </button>
          <button className={styles.exportOption}>
            <div className={styles.exportIcon}>XLSX</div>
            <div className={styles.exportText}>
              <div className={styles.exportTitle}>Excel Report</div>
              <div className={styles.exportDescription}>Formatted spreadsheet</div>
            </div>
          </button>
        </div>
      </div>
    </div>
  );
}
