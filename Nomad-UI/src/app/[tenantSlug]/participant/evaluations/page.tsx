'use client';

import React, { useEffect, useState, use } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import {
  MagnifyingGlassIcon,
  FunnelIcon,
  CheckCircleIcon,
  ClockIcon,
} from '@heroicons/react/24/outline';

interface Evaluation {
  AssignmentId: string;
  SurveyId: string;
  SubjectName: string;
  SurveyTitle: string;
  Status: 'Pending' | 'InProgress' | 'Completed';
  AssignedDate: string;
  CompletedDate?: string;
  DueDate?: string;
}

type FilterStatus = 'All' | 'Pending' | 'InProgress' | 'Completed';

interface AssignedEvaluationsProps {
  params: Promise<{ tenantSlug: string }>;
}

export default function AssignedEvaluations({ params }: AssignedEvaluationsProps) {
  const { tenantSlug } = use(params);
  const router = useRouter();
  const { user, token } = useAuth();
  const [evaluations, setEvaluations] = useState<Evaluation[]>([]);
  const [filteredEvaluations, setFilteredEvaluations] = useState<Evaluation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<FilterStatus>('All');
  const [sortBy, setSortBy] = useState<'subject' | 'survey' | 'status' | 'date'>('date');

  useEffect(() => {
    if (user && tenantSlug) {
      loadEvaluations();
    }
  }, [user, tenantSlug]);

  useEffect(() => {
    filterAndSortEvaluations();
  }, [evaluations, searchQuery, statusFilter, sortBy]);

  const loadEvaluations = async () => {
    try {
      setIsLoading(true);

      // Build query params
      const params = new URLSearchParams();
      if (statusFilter !== 'All') {
        params.append('status', statusFilter);
      }
      if (searchQuery) {
        params.append('search', searchQuery);
      }

      const queryString = params.toString();
      const url = `/api/${tenantSlug}/participant/evaluations${queryString ? `?${queryString}` : ''}`;

      console.log('Fetching evaluations from:', url);
      const response = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Evaluations API error:', response.status, errorText);
        throw new Error(`Failed to fetch evaluations: ${response.status}`);
      }

      const data = await response.json();
      console.log('Evaluations data:', data);
      setEvaluations(data);
    } catch (error) {
      console.error('Error loading evaluations:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const filterAndSortEvaluations = () => {
    let filtered = [...evaluations];

    // Apply status filter
    if (statusFilter !== 'All') {
      filtered = filtered.filter((e) => e.Status === statusFilter);
    }

    // Apply search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (e) =>
          e.SubjectName.toLowerCase().includes(query) ||
          e.SurveyTitle.toLowerCase().includes(query)
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'subject':
          return a.SubjectName.localeCompare(b.SubjectName);
        case 'survey':
          return a.SurveyTitle.localeCompare(b.SurveyTitle);
        case 'status':
          return a.Status.localeCompare(b.Status);
        case 'date':
        default:
          return new Date(b.AssignedDate).getTime() - new Date(a.AssignedDate).getTime();
      }
    });

    setFilteredEvaluations(filtered);
  };

  const getStatusBadge = (status: string) => {
    if (status === 'Completed') {
      return (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
          <CheckCircleIcon className="h-4 w-4 mr-1" />
          Completed
        </span>
      );
    }
    if (status === 'InProgress') {
      return (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
          <ClockIcon className="h-4 w-4 mr-1" />
          In Progress
        </span>
      );
    }
    return (
      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
        <ClockIcon className="h-4 w-4 mr-1" />
        Pending
      </span>
    );
  };

  const pendingCount = evaluations.filter((e) => e.Status === 'Pending').length;
  const inProgressCount = evaluations.filter((e) => e.Status === 'InProgress').length;
  const completedCount = evaluations.filter((e) => e.Status === 'Completed').length;

  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-7xl mx-auto">
          {/* Header */}
          <div className="mb-6">
            <h1 className="text-2xl font-bold text-black">Assigned Evaluations</h1>
            <p className="text-sm text-black mt-1">
              View and complete all surveys assigned to you
            </p>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
            <div className="bg-white rounded-lg border border-gray-200 p-4">
              <p className="text-sm font-medium text-black">Total Assigned</p>
              <p className="text-2xl font-bold text-black mt-1">{evaluations.length}</p>
            </div>
            <div className="bg-white rounded-lg border border-gray-200 p-4">
              <p className="text-sm font-medium text-black">Pending</p>
              <p className="text-2xl font-bold text-blue-600 mt-1">{pendingCount}</p>
            </div>
            <div className="bg-white rounded-lg border border-gray-200 p-4">
              <p className="text-sm font-medium text-black">Completed</p>
              <p className="text-2xl font-bold text-green-600 mt-1">{completedCount}</p>
            </div>
          </div>

          {/* Filters and Search */}
          <div className="bg-white rounded-lg border border-gray-200 p-4 mb-6">
            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
              {/* Search */}
              <div className="flex-1 max-w-md">
                <div className="relative">
                  <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
                  <input
                    type="text"
                    placeholder="Search by subject or survey title..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg text-sm text-black placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
              </div>

              {/* Filters */}
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  <FunnelIcon className="h-5 w-5 text-black" />
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value as FilterStatus)}
                    className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-black focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  >
                    <option value="All">All Status</option>
                    <option value="Pending">Pending</option>
                    <option value="InProgress">In Progress</option>
                    <option value="Completed">Completed</option>
                  </select>
                </div>

                <select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value as typeof sortBy)}
                  className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-black focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
                  <option value="date">Sort by Date</option>
                  <option value="subject">Sort by Subject</option>
                  <option value="survey">Sort by Survey</option>
                  <option value="status">Sort by Status</option>
                </select>
              </div>
            </div>
          </div>

          {/* Evaluations Table */}
          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            {isLoading ? (
              <div className="px-6 py-12 text-center">
                <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
                <p className="text-sm text-black mt-4">Loading evaluations...</p>
              </div>
            ) : filteredEvaluations.length === 0 ? (
              <div className="px-6 py-12 text-center">
                <p className="text-sm font-medium text-black">No evaluations found</p>
                <p className="text-sm text-black mt-1">
                  {searchQuery || statusFilter !== 'All'
                    ? 'Try adjusting your filters'
                    : 'You have no assigned evaluations at the moment'}
                </p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-black uppercase tracking-wider">
                        Subject Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-black uppercase tracking-wider">
                        Survey Title
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-black uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-black uppercase tracking-wider">
                        Due Date
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-black uppercase tracking-wider">
                        Action
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {filteredEvaluations.map((evaluation) => (
                      <tr key={evaluation.AssignmentId} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm font-medium text-black">{evaluation.SubjectName}</div>
                        </td>
                        <td className="px-6 py-4">
                          <div className="text-sm text-black">{evaluation.SurveyTitle}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          {getStatusBadge(evaluation.Status)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-black">
                            {evaluation.DueDate
                              ? new Date(evaluation.DueDate).toLocaleDateString()
                              : '-'}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          {evaluation.Status === 'Completed' ? (
                            <button
                              disabled
                              // onClick={() => router.push(`/${tenantSlug}/participant/submissions/${evaluation.AssignmentId}`)}
                              className="px-4 py-2 border border-gray-300 text-black rounded-lg hover:bg-gray-50 transition-colors"
                            >
                              Completed
                            </button>
                          ) : (
                            <button
                              onClick={() => router.push(`/${tenantSlug}/participant/evaluations/${evaluation.AssignmentId}`)}
                              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                            >
                              {evaluation.Status === 'InProgress' ? 'Continue' : 'Start'}
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}

