'use client';

import React, { useEffect, useState, use } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import {
  MagnifyingGlassIcon,
  DocumentTextIcon,
  CalendarIcon,
  UserIcon,
  IdentificationIcon,
  CheckCircleIcon
} from '@heroicons/react/24/outline';
import { toTitleCase } from '@/lib/stringUtils';

interface Submission {
  SubmissionId: string;
  SubjectName: string;
  SurveyTitle: string;
  CompletedDate: string;
  CompletedAt: string;
  SubmittedAt: string;
  RelationshipType: string;
}

interface MySubmissionsProps {
  params: Promise<{ tenantSlug: string }>;
}

export default function MySubmissions({ params }: MySubmissionsProps) {
  const { tenantSlug } = use(params);
  const router = useRouter();
  const { user, token } = useAuth();
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [filteredSubmissions, setFilteredSubmissions] = useState<Submission[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    if (user && tenantSlug) {
      loadSubmissions();
    }
  }, [user, tenantSlug]);

  useEffect(() => {
    filterSubmissions();
  }, [submissions, searchQuery]);

  const loadSubmissions = async () => {
    try {
      setIsLoading(true);

      // Build query params
      const params = new URLSearchParams();
      if (searchQuery) {
        params.append('search', searchQuery);
      }

      const queryString = params.toString();
      const url = `/api/${tenantSlug}/participant/submissions${queryString ? `?${queryString}` : ''}`;

      console.log('Fetching submissions from:', url);
      const response = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Submissions API error:', response.status, errorText);
        throw new Error(`Failed to fetch submissions: ${response.status}`);
      }

      const data = await response.json();
      console.log('Submissions data:', data);
      setSubmissions(data);
    } catch (error) {
      console.error('Error loading submissions:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const filterSubmissions = () => {
    let filtered = [...submissions];

    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (s) =>
          s.SubjectName.toLowerCase().includes(query) ||
          s.SurveyTitle.toLowerCase().includes(query)
      );
    }

    // Sort by date (most recent first)
    filtered.sort((a, b) => new Date(b.CompletedDate || b.CompletedAt || b.SubmittedAt).getTime() - new Date(a.CompletedDate || a.CompletedAt || a.SubmittedAt).getTime());

    setFilteredSubmissions(filtered);
  };

  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-7xl mx-auto">
          {/* Header */}
          <div className="mb-6">
            <h1 className="text-2xl font-bold text-black">My Submissions</h1>
            <p className="text-sm text-black mt-1">
              View all your completed evaluation submissions
            </p>
          </div>

          {/* Stats */}
          <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className="h-12 w-12 bg-green-100 rounded-lg flex items-center justify-center">
                  <DocumentTextIcon className="h-6 w-6 text-green-600" />
                </div>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-black">Total Submissions</p>
                <p className="text-2xl font-bold text-black mt-1">{submissions.length}</p>
              </div>
            </div>
          </div>

          {/* Search */}
          <div className="bg-white rounded-lg border border-gray-200 p-4 mb-6">
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

          {/* Submissions List */}
          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            {isLoading ? (
              <div className="px-6 py-12 text-center">
                <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
                <p className="text-sm text-black mt-4">Loading submissions...</p>
              </div>
            ) : filteredSubmissions.length === 0 ? (
              <div className="px-6 py-12 text-center">
                <DocumentTextIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-sm font-medium text-black">No submissions found</p>
                <p className="text-sm text-black mt-1">
                  {searchQuery
                    ? 'Try adjusting your search'
                    : 'You have not completed any evaluations yet'}
                </p>
              </div>
            ) : (
              <>
                {/* Desktop Table */}
                <div className="hidden md:block overflow-x-auto">
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
                          Relationship
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-black uppercase tracking-wider">
                          Submitted Date
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {filteredSubmissions.map((submission) => (
                        <tr key={submission.SubmissionId} className="hover:bg-gray-50 transition-colors">
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm font-medium text-black">{toTitleCase(submission.SubjectName)}</div>
                          </td>
                          <td className="px-6 py-4">
                            <div className="text-sm text-black">{submission.SurveyTitle}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm text-black capitalize">{submission.RelationshipType}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="flex items-center text-sm text-black">
                              <CalendarIcon className="h-4 w-4 mr-2 text-gray-400" />
                              {submission.CompletedAt || submission.SubmittedAt || submission.CompletedDate
                                ? new Date(submission.CompletedAt || submission.SubmittedAt || submission.CompletedDate).toLocaleDateString('en-US', {
                                  year: 'numeric',
                                  month: 'short',
                                  day: 'numeric'
                                })
                                : 'N/A'}
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Mobile Card View */}
                <div className="md:hidden divide-y divide-gray-200">
                  {filteredSubmissions.map((submission) => (
                    <div key={submission.SubmissionId} className="p-4 space-y-3">
                      <div>
                        <h3 className="text-sm font-bold text-black">{submission.SurveyTitle}</h3>
                        <div className="flex items-center text-xs text-gray-500 mt-1">
                          <UserIcon className="h-3 w-3 mr-1" />
                          <span>Subject: {toTitleCase(submission.SubjectName)}</span>
                        </div>
                      </div>

                      <div className="grid grid-cols-2 gap-2 text-xs">
                        <div className="flex items-center">
                          <IdentificationIcon className="h-3 w-3 mr-1 text-gray-400" />
                          <span className="text-gray-500 mr-1">Role:</span>
                          <span className="text-black capitalize">{submission.RelationshipType}</span>
                        </div>
                        <div className="flex items-center">
                          <CalendarIcon className="h-3 w-3 mr-1 text-gray-400" />
                          <span className="text-black">
                            {submission.CompletedAt || submission.SubmittedAt || submission.CompletedDate
                              ? new Date(submission.CompletedAt || submission.SubmittedAt || submission.CompletedDate).toLocaleDateString('en-US', {
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric'
                              })
                              : 'N/A'}
                          </span>
                        </div>
                      </div>

                      <div className="pt-1">
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-green-100 text-green-800">
                          <CheckCircleIcon className="h-3 w-3 mr-1" />
                          Submitted
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}
