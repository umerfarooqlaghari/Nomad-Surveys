'use client';

import React, { useEffect, useState, use } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import {
  ClipboardDocumentListIcon,
  CheckCircleIcon,
  ClockIcon,
  ArrowRightIcon,
} from '@heroicons/react/24/outline';

interface DashboardStats {
  PendingCount: number;
  InProgressCount: number;
  CompletedCount: number;
  TotalAssigned: number;
}

interface PendingEvaluation {
  AssignmentId: string;
  SurveyId: string;
  SubjectName: string;
  SurveyTitle: string;
  DueDate?: string;
  Status: string;
  RelationshipType: string;
}

interface ParticipantDashboardProps {
  params: Promise<{ tenantSlug: string }>;
}

export default function ParticipantDashboard({ params }: ParticipantDashboardProps) {
  const { tenantSlug } = use(params);
  const router = useRouter();
  const { user, token } = useAuth();
  const [stats, setStats] = useState<DashboardStats>({
    PendingCount: 0,
    InProgressCount: 0,
    CompletedCount: 0,
    TotalAssigned: 0,
  });
  const [pendingEvaluations, setPendingEvaluations] = useState<PendingEvaluation[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (user && tenantSlug) {
      loadDashboardData();
    }
  }, [user, tenantSlug]);

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);

      // Fetch dashboard data from API
      console.log('Fetching dashboard from:', `/api/${tenantSlug}/participant/dashboard`);
      const response = await fetch(`/api/${tenantSlug}/participant/dashboard`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Dashboard API error:', response.status, errorText);
        throw new Error(`Failed to fetch dashboard data: ${response.status}`);
      }

      const data = await response.json();
      console.log('Dashboard data:', data);
      setStats(data.Stats);
      setPendingEvaluations(data.PendingEvaluations);
    } catch (error) {
      console.error('Error loading dashboard data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good Morning';
    if (hour < 18) return 'Good Afternoon';
    return 'Good Evening';
  };

  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-7xl mx-auto">
          {/* Greeting */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-black">
              {getGreeting()}, {user?.FirstName}!
            </h1>
            <p className="text-sm text-black mt-1">
              Here&apos;s an overview of your evaluation tasks
            </p>
          </div>

          {/* Stats Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            {/* Pending Evaluations */}
            <div className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-black">Pending Evaluations</p>
                  <p className="text-3xl font-bold text-black mt-2">{stats.PendingCount}</p>
                </div>
                <div className="h-12 w-12 bg-blue-100 rounded-lg flex items-center justify-center">
                  <ClockIcon className="h-6 w-6 text-blue-600" />
                </div>
              </div>
              <button
                onClick={() => router.push('/participant/evaluations')}
                className="mt-4 text-sm font-medium text-blue-600 hover:text-blue-700 flex items-center"
              >
                View all
                <ArrowRightIcon className="h-4 w-4 ml-1" />
              </button>
            </div>

            {/* Completed Evaluations */}
            <div className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-black">Completed</p>
                  <p className="text-3xl font-bold text-black mt-2">{stats.CompletedCount}</p>
                </div>
                <div className="h-12 w-12 bg-green-100 rounded-lg flex items-center justify-center">
                  <CheckCircleIcon className="h-6 w-6 text-green-600" />
                </div>
              </div>
              <button
                onClick={() => router.push(`/${tenantSlug}/participant/evaluations`)}
                className="mt-4 text-sm font-medium text-blue-600 hover:text-blue-700 flex items-center"
              >
                View history
                <ArrowRightIcon className="h-4 w-4 ml-1" />
              </button>
            </div>

            {/* Total Assigned */}
            <div className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-black">Total Assigned</p>
                  <p className="text-3xl font-bold text-black mt-2">{stats.TotalAssigned}</p>
                </div>
                <div className="h-12 w-12 bg-blue-100 rounded-lg flex items-center justify-center">
                  <ClipboardDocumentListIcon className="h-6 w-6 text-blue-600" />
                </div>
              </div>
              <div className="mt-4 text-sm text-black">
                {stats.PendingCount > 0 ? (
                  <span className="text-black font-medium">{stats.PendingCount} pending</span>
                ) : (
                  <span className="text-green-600 font-medium">All caught up!</span>
                )}
              </div>
            </div>
          </div>

          {/* Pending Evaluations List */}
          <div className="bg-white rounded-lg border border-gray-200">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-black">Pending Evaluations</h2>
              <p className="text-sm text-black mt-1">Quick access to your pending evaluation forms</p>
            </div>

            {isLoading ? (
              <div className="px-6 py-12 text-center">
                <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
                <p className="text-sm text-black mt-4">Loading evaluations...</p>
              </div>
            ) : pendingEvaluations.length === 0 ? (
              <div className="px-6 py-12 text-center">
                <CheckCircleIcon className="h-12 w-12 text-green-600 mx-auto mb-4" />
                <p className="text-sm font-medium text-black">All caught up!</p>
                <p className="text-sm text-black mt-1">You have no pending evaluations at the moment.</p>
              </div>
            ) : (
              <div className="divide-y divide-gray-200">
                {pendingEvaluations.map((evaluation) => (
                  <div
                    key={evaluation.AssignmentId}
                    className="px-6 py-4 hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <h3 className="text-sm font-semibold text-black">{evaluation.SurveyTitle}</h3>
                        <p className="text-sm text-black mt-1">
                          Subject: <span className="font-medium text-black">{evaluation.SubjectName}</span>
                        </p>
                        <p className="text-sm text-black mt-1">
                          Relationship: <span className="font-medium text-black capitalize">{evaluation.RelationshipType}</span>
                        </p>
                        {evaluation.DueDate && (
                          <p className="text-xs text-black mt-1">
                            Due: {new Date(evaluation.DueDate).toLocaleDateString()}
                          </p>
                        )}
                      </div>
                      <button
                        onClick={() => router.push(`/${tenantSlug}/participant/evaluations/${evaluation.AssignmentId}`)}
                        className="ml-4 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
                      >
                        Start
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {pendingEvaluations.length > 0 && (
              <div className="px-6 py-4 border-t border-gray-200 bg-gray-50">
                <button
                  onClick={() => router.push(`/${tenantSlug}/participant/evaluations`)}
                  className="text-sm font-medium text-blue-600 hover:text-blue-700 flex items-center"
                >
                  View all evaluations
                  <ArrowRightIcon className="h-4 w-4 ml-1" />
                </button>
              </div>
            )}
          </div>
        </div>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}
