'use client';

import React from 'react';
import styles from './OverviewTab.module.css';
import Image from 'next/image';


export default function OverviewTab() {
  // Static placeholder data
  const stats = [
    {
      name: 'Companies Registered',
      value: '24',
      change: '+12%',
      changeType: 'increase',
      icon: '/Icons/building-check.svg',
    },
    {
      name: 'Users Registered',
      value: '1,247',
      change: '+8%',
      changeType: 'increase',
      icon: '/Icons/person-check.svg',
    },
    {
      name: 'Surveys Completed',
      value: '89',
      change: '+23%',
      changeType: 'increase',
      icon: '/Icons/ui-checks.svg',
    },
    {
      name: 'Survey Completion Rate',
      value: '73.2%',
      change: '+5.1%',
      changeType: 'increase',
      icon: '/Icons/percent.svg',
    },
  ];

  const recentActivity = [
    { id: 1, action: 'New company registered', company: 'TechCorp Inc.', time: '2 hours ago' },
    { id: 2, action: 'Survey completed', company: 'Acme Corp', time: '4 hours ago' },
    { id: 3, action: 'New user registered', company: 'Global Solutions', time: '6 hours ago' },
    { id: 4, action: 'Survey created', company: 'Innovation Labs', time: '1 day ago' },
    { id: 5, action: 'Company profile updated', company: 'Future Tech', time: '2 days ago' },
  ];

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
                      <div className={`${styles.statChange} ${
                        stat.changeType === 'increase' ? styles.statChangeIncrease : styles.statChangeDecrease
                      }`}>
                        {stat.changeType === 'increase' ? '↗' : '↘'}
                        {stat.change}
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
          <div className="h-64 flex items-center justify-center bg-gray-50 rounded-lg">
            <div className="text-center">
<div className={styles.statIcon}>
                  <Image
                    src="/Icons/bar-chart.svg"
                    alt={""}
                    width={32}
                    height={32}
                    className="object-contain"
                  />
                </div>
            </div>
          </div>
        </div>

        {/* Survey Completion Chart */}
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Survey Completion Trends</h3>
          <div className="h-64 flex items-center justify-center bg-gray-50 rounded-lg">
            <div className="text-center">
<div className={styles.statIcon}>
                  <Image
                    src="/Icons/graph-up-arrow.svg"
                    alt={""}
                    width={32}
                    height={32}
                    className="object-contain"
                  />
                </div>
            </div>
          </div>
        </div>
      </div>

      {/* Recent Activity */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Recent Activity</h3>
        </div>
        <div className="divide-y divide-gray-200">
          {recentActivity.map((activity) => (
            <div key={activity.id} className="px-6 py-4 flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <div className="flex-shrink-0">
                  <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-900">{activity.action}</p>
                  <p className="text-sm text-gray-500">{activity.company}</p>
                </div>
              </div>
              <div className="text-sm text-gray-400">{activity.time}</div>
            </div>
          ))}
        </div>
        <div className="px-6 py-3 bg-gray-50 text-center">
          <button className="text-sm text-blue-600 hover:text-blue-500">
            View all activity
          </button>
        </div>
      </div>
    </div>
  );
}
