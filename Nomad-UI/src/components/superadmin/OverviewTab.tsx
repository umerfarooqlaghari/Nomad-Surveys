'use client';

import { useEffect, useState } from 'react';
import styles from './OverviewTab.module.css';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';
import { Bar, Line } from 'react-chartjs-2';

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  Title,
  Tooltip,
  Legend
);

interface StatItem {
  value: number;
  change: number;
  changeType: string;
  label?: string;
}

interface ChartDataPoint {
  label: string;
  value: number;
}

interface DashboardData {
  companiesRegistered: StatItem;
  usersRegistered: StatItem;
  surveysCompleted: StatItem;
  surveyCompletionRate: StatItem;
  userGrowthData: ChartDataPoint[];
  surveyCompletionTrends: ChartDataPoint[];
}

export default function OverviewTab() {
  const { token } = useAuth();
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      if (!token) return;

      try {
        setIsLoading(true);
        const response = await fetch('/api/superadmin/analytics', {
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error('Failed to fetch dashboard data');
        }

        const data = await response.json();
        console.log('Dashboard API response:', data);

        // Helper to transform PascalCase StatItem to camelCase
        const transformStatItem = (item: { Value?: number; value?: number; Change?: number; change?: number; ChangeType?: string; changeType?: string; Label?: string; label?: string } | undefined): StatItem => ({
          value: item?.Value ?? item?.value ?? 0,
          change: item?.Change ?? item?.change ?? 0,
          changeType: item?.ChangeType ?? item?.changeType ?? 'increase',
          label: item?.Label ?? item?.label,
        });

        // Transform PascalCase to camelCase
        setDashboardData({
          companiesRegistered: transformStatItem(data.CompaniesRegistered || data.companiesRegistered),
          usersRegistered: transformStatItem(data.UsersRegistered || data.usersRegistered),
          surveysCompleted: transformStatItem(data.SurveysCompleted || data.surveysCompleted),
          surveyCompletionRate: transformStatItem(data.SurveyCompletionRate || data.surveyCompletionRate),
          userGrowthData: (data.UserGrowthData || data.userGrowthData || []).map((d: { Label?: string; label?: string; Value?: number; value?: number }) => ({
            label: d.Label || d.label || '',
            value: d.Value ?? d.value ?? 0,
          })),
          surveyCompletionTrends: (data.SurveyCompletionTrends || data.surveyCompletionTrends || []).map((d: { Label?: string; label?: string; Value?: number; value?: number }) => ({
            label: d.Label || d.label || '',
            value: d.Value ?? d.value ?? 0,
          })),
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchDashboardData();
  }, [token]);

  const formatValue = (value: number | undefined, isPercentage = false): string => {
    const val = value ?? 0;
    if (isPercentage) return `${val}%`;
    return val.toLocaleString();
  };

  const formatChange = (change: number | undefined): string => {
    const val = change ?? 0;
    const sign = val >= 0 ? '+' : '';
    return `${sign}${val}%`;
  };

  const getStatItem = (item: StatItem | undefined) => ({
    value: item?.value ?? 0,
    change: item?.change ?? 0,
    changeType: item?.changeType ?? 'increase',
    label: item?.label,
  });

  const stats = dashboardData ? [
    {
      name: 'Companies Registered',
      value: formatValue(getStatItem(dashboardData.companiesRegistered).value),
      change: formatChange(getStatItem(dashboardData.companiesRegistered).change),
      changeType: getStatItem(dashboardData.companiesRegistered).changeType,
      label: getStatItem(dashboardData.companiesRegistered).label,
      icon: '/Icons/building-check.svg',
    },
    {
      name: 'Users Registered',
      value: formatValue(getStatItem(dashboardData.usersRegistered).value),
      change: formatChange(getStatItem(dashboardData.usersRegistered).change),
      changeType: getStatItem(dashboardData.usersRegistered).changeType,
      label: getStatItem(dashboardData.usersRegistered).label,
      icon: '/Icons/person-check.svg',
    },
    {
      name: 'Surveys Completed',
      value: formatValue(getStatItem(dashboardData.surveysCompleted).value),
      change: formatChange(getStatItem(dashboardData.surveysCompleted).change),
      changeType: getStatItem(dashboardData.surveysCompleted).changeType,
      label: getStatItem(dashboardData.surveysCompleted).label,
      icon: '/Icons/ui-checks.svg',
    },
    {
      name: 'Survey Completion Rate',
      value: formatValue(getStatItem(dashboardData.surveyCompletionRate).value, true),
      change: formatChange(getStatItem(dashboardData.surveyCompletionRate).change),
      changeType: getStatItem(dashboardData.surveyCompletionRate).changeType,
      label: getStatItem(dashboardData.surveyCompletionRate).label,
      icon: '/Icons/percent.svg',
    },
  ] : [];

  // Chart configurations
  const userGrowthChartData = {
    labels: dashboardData?.userGrowthData.map(d => d.label) || [],
    datasets: [
      {
        label: 'New Users',
        data: dashboardData?.userGrowthData.map(d => d.value) || [],
        backgroundColor: 'rgba(59, 130, 246, 0.8)',
        borderColor: 'rgb(59, 130, 246)',
        borderWidth: 1,
        borderRadius: 4,
      },
    ],
  };

  const surveyTrendsChartData = {
    labels: dashboardData?.surveyCompletionTrends.map(d => d.label) || [],
    datasets: [
      {
        label: 'Completion Rate (%)',
        data: dashboardData?.surveyCompletionTrends.map(d => d.value) || [],
        borderColor: 'rgb(16, 185, 129)',
        backgroundColor: 'rgba(16, 185, 129, 0.1)',
        tension: 0.3,
        fill: true,
        pointBackgroundColor: 'rgb(16, 185, 129)',
        pointBorderColor: '#fff',
        pointBorderWidth: 2,
        pointRadius: 4,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          stepSize: 1,
        },
      },
    },
  };

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.statsGrid}>
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className={styles.statCard}>
              <div className={styles.statCardContent}>
                <div className="animate-pulse flex items-center">
                  <div className="w-10 h-10 bg-gray-200 rounded-lg mr-4"></div>
                  <div className="flex-1">
                    <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
                    <div className="h-6 bg-gray-200 rounded w-1/2"></div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
          Error loading dashboard data: {error}
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {/* Stats Grid */}
      <div className={styles.statsGrid}>
        {stats.map((stat) => (
          <div key={stat.name} className={styles.statCard}>
            <div className={styles.statCardContent}>
              <div className={styles.statCardInner}>
                <div className={styles.statIcon}>
                  <Image
                    src={stat.icon}
                    alt={""}
                    width={24}
                    height={24}
                    className="object-contain"
                  />
                </div>
                <div className={styles.statDetails}>
                  <dl>
                    <dt className={styles.statName}>
                      {stat.name}
                    </dt>
                    <dd className={styles.statValueContainer}>
                      <div className={styles.statValue}>
                        {stat.value}
                      </div>
                      <div className={`${styles.statChange} ${stat.changeType === 'increase' ? styles.statChangeIncrease : styles.statChangeDecrease
                        }`}>
                        {stat.changeType === 'increase' ? '↗' : '↘'}
                        {stat.change}
                        {stat.label && <span className="ml-1 text-[10px] opacity-70 font-normal">({stat.label})</span>}
                      </div>
                    </dd>
                  </dl>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* User Growth Chart */}
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">User Growth</h3>
          <div className="h-64">
            <Bar data={userGrowthChartData} options={chartOptions} />
          </div>
        </div>

        {/* Survey Completion Chart */}
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Survey Completion Trends</h3>
          <div className="h-64">
            <Line data={surveyTrendsChartData} options={chartOptions} />
          </div>
        </div>
      </div>
    </div>
  );
}
