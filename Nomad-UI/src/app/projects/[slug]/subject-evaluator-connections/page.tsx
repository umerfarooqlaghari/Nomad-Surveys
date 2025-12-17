'use client';

import React, { useState, useEffect } from 'react';
import { useParams } from 'next/navigation';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import ProtectedRoute from '@/components/ProtectedRoute';
import LogoutConfirmationModal from '@/components/modals/LogoutConfirmationModal';
import toast from 'react-hot-toast';

interface Subject {
  Id: string;
  FullName: string;
  Email: string;
  EmployeeIdString: string;
  Designation?: string;
  IsActive: boolean;
}

interface Evaluator {
  Id: string;
  FullName: string;
  Email: string;
  EmployeeIdString: string;
  Designation?: string;
  IsActive: boolean;
}

interface Connection {
  Id: string;
  SubjectId: string;
  EvaluatorId: string;
  Relationship: string;
  SubjectFullName?: string;
  SubjectEmail?: string;
  EvaluatorFullName?: string;
  EvaluatorEmail?: string;
}

const relationshipTypes = [
  'Manager',
  'Direct Report',
  'Peer',
  'Colleague',
  'Team Lead',
  'Supervisor',
  'Other'
];

export default function SubjectEvaluatorConnections() {
  const params = useParams();
  const { user, logout, token } = useAuth();
  const projectSlug = params.slug as string;

  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [evaluators, setEvaluators] = useState<Evaluator[]>([]);
  const [connections, setConnections] = useState<Connection[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [selectedSubject, setSelectedSubject] = useState('');
  const [selectedEvaluator, setSelectedEvaluator] = useState('');
  const [selectedRelationship, setSelectedRelationship] = useState('');
  const [showLogoutModal, setShowLogoutModal] = useState(false);

  // Search states
  const [subjectSearch, setSubjectSearch] = useState('');
  const [evaluatorSearch, setEvaluatorSearch] = useState('');

  useEffect(() => {
    if (token) {
      loadData();
    }
  }, [projectSlug, token]);

  const handleLogoutClick = () => {
    setShowLogoutModal(true);
  };

  const handleLogoutConfirm = () => {
    setShowLogoutModal(false);
    logout();
  };

  const handleLogoutCancel = () => {
    setShowLogoutModal(false);
  };

  const loadData = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      // Load subjects
      const subjectsResponse = await fetch(`/api/${projectSlug}/subjects`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (subjectsResponse.ok) {
        const subjectsData = await subjectsResponse.json();
        setSubjects(subjectsData || []);
      }

      // Load evaluators
      const evaluatorsResponse = await fetch(`/api/${projectSlug}/evaluators`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (evaluatorsResponse.ok) {
        const evaluatorsData = await evaluatorsResponse.json();
        setEvaluators(evaluatorsData || []);
      }

      // Load existing connections (if you have an endpoint for this)
      // For now, we'll leave connections empty
      setConnections([]);
    } catch (error) {
      console.error('Error loading data:', error);
      toast.error('Failed to load data');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateConnection = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSubject || !selectedEvaluator || !selectedRelationship || !token) {
      toast.error('Please fill all fields');
      return;
    }

    setIsLoading(true);
    try {
      const response = await fetch(`/api/${projectSlug}/subject-evaluators/subjects/${selectedSubject}/evaluators`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          Evaluators: [{
            EvaluatorId: selectedEvaluator,
            Relationship: selectedRelationship
          }]
        })
      });

      if (response.ok) {
        toast.success('Connection created successfully');
        setShowCreateForm(false);
        resetForm();
        loadData();
      } else {
        const error = await response.json();
        toast.error(error.message || 'Failed to create connection');
      }
    } catch (error) {
      console.error('Error creating connection:', error);
      toast.error('Failed to create connection');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteConnection = async (connectionId: string, subjectId: string, evaluatorId: string) => {
    if (!confirm('Are you sure you want to delete this connection?') || !token) return;

    setIsLoading(true);
    try {
      const response = await fetch(`/api/${projectSlug}/subject-evaluators/subjects/${subjectId}/evaluators/${evaluatorId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (response.ok) {
        toast.success('Connection deleted');
        loadData();
      } else {
        toast.error('Failed to delete connection');
      }
    } catch (error) {
      console.error('Error deleting connection:', error);
      toast.error('Failed to delete connection');
    } finally {
      setIsLoading(false);
    }
  };

  const resetForm = () => {
    setSelectedSubject('');
    setSelectedEvaluator('');
    setSelectedRelationship('');
    setSubjectSearch('');
    setEvaluatorSearch('');
  };

  return (
    <ProtectedRoute allowedRoles={['SuperAdmin', 'TenantAdmin']}>
      <div className="min-h-screen bg-gray-50">
        {/* Navigation */}
        <nav className="bg-white shadow-sm border-b border-blue-700">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between h-16">
              <div className="flex items-center">
                <Image
                  src="/logos/logo-small.png"
                  alt="Nomad Surveys"
                  width={150}
                  height={60}
                  className="h-12 w-auto"
                />
                <div className="ml-6">
                  <h1 className="text-xl font-semibold text-black">
                    Subject-Evaluator Connections
                  </h1>
                  <h1 className="text-sm text-black opacity-90">
                    Project: {projectSlug}
                  </h1>
                </div>
              </div>

              <div className="flex items-center space-x-4">
                <div className="text-sm text-black">
                  <span className="font-medium">{user?.fullName}</span>
                </div>
                <button
                  onClick={() => window.close()}
                  className="bg-gray-500 hover:bg-gray-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors mr-2"
                >
                  Close
                </button>
                <button
                  onClick={handleLogoutClick}
                  className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
                >
                  Logout
                </button>
              </div>
            </div>
          </div>
        </nav>

        {/* Main Content */}
        <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
          <div className="px-4 py-6 sm:px-0 space-y-6">
            
            {/* Header */}
            <div className="bg-white shadow rounded-lg p-6">
              <div className="flex justify-between items-center mb-4">
                <div>
                  <h2 className="text-2xl font-bold text-gray-900">Subject-Evaluator Connections</h2>
                  <p className="text-gray-600">Manage relationships between subjects and evaluators for 360-degree feedback</p>
                </div>
                <button
                  onClick={() => setShowCreateForm(true)}
                  className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                >
                  Create Connection
                </button>
              </div>

              {/* Create Connection Form */}
              {showCreateForm && (
                <div className="border-t pt-6 mt-6 bg-gray-50 p-6 rounded-lg">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Create New Connection</h3>
                  <form onSubmit={handleCreateConnection} className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Subject</label>
                        <input
                          type="text"
                          placeholder="Search subjects..."
                          value={subjectSearch}
                          onChange={(e) => setSubjectSearch(e.target.value)}
                          className="mb-2 block w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
                        />
                        <select
                          value={selectedSubject}
                          onChange={(e) => setSelectedSubject(e.target.value)}
                          className="block w-full border border-gray-300 rounded-md px-3 py-2 bg-white"
                          required
                          size={5}
                        >
                          <option value="">Select Subject</option>
                          {subjects
                            .filter(subject =>
                              subjectSearch === '' ||
                              subject.FullName.toLowerCase().includes(subjectSearch.toLowerCase()) ||
                              subject.Email.toLowerCase().includes(subjectSearch.toLowerCase()) ||
                              subject.EmployeeIdString.toLowerCase().includes(subjectSearch.toLowerCase())
                            )
                            .map((subject) => (
                              <option key={subject.Id} value={subject.Id}>
                                {subject.FullName} ({subject.EmployeeIdString})
                              </option>
                            ))}
                        </select>
                      </div>

                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Evaluator</label>
                        <input
                          type="text"
                          placeholder="Search evaluators..."
                          value={evaluatorSearch}
                          onChange={(e) => setEvaluatorSearch(e.target.value)}
                          className="mb-2 block w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
                        />
                        <select
                          value={selectedEvaluator}
                          onChange={(e) => setSelectedEvaluator(e.target.value)}
                          className="block w-full border border-gray-300 rounded-md px-3 py-2 bg-white"
                          required
                          size={5}
                        >
                          <option value="">Select Evaluator</option>
                          {evaluators
                            .filter(evaluator =>
                              evaluatorSearch === '' ||
                              evaluator.FullName.toLowerCase().includes(evaluatorSearch.toLowerCase()) ||
                              evaluator.Email.toLowerCase().includes(evaluatorSearch.toLowerCase()) ||
                              evaluator.EmployeeIdString.toLowerCase().includes(evaluatorSearch.toLowerCase())
                            )
                            .map((evaluator) => (
                              <option key={evaluator.Id} value={evaluator.Id}>
                                {evaluator.FullName} ({evaluator.EmployeeIdString})
                              </option>
                            ))}
                        </select>
                      </div>

                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Relationship</label>
                        <select
                          value={selectedRelationship}
                          onChange={(e) => setSelectedRelationship(e.target.value)}
                          className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 bg-white"
                          required
                        >
                          <option value="">Select Relationship</option>
                          {relationshipTypes.map((type) => (
                            <option key={type} value={type}>
                              {type}
                            </option>
                          ))}
                        </select>
                      </div>
                    </div>

                    <div className="flex justify-end space-x-3">
                      <button
                        type="button"
                        onClick={() => {
                          setShowCreateForm(false);
                          resetForm();
                        }}
                        className="bg-gray-300 hover:bg-gray-400 text-gray-700 px-4 py-2 rounded-md"
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md"
                      >
                        Create Connection
                      </button>
                    </div>
                  </form>
                </div>
              )}
            </div>

            {/* Connections List */}
            <div className="bg-white shadow rounded-lg p-6">
              <h3 className="text-lg font-medium text-gray-900 mb-4">Existing Connections</h3>
              {isLoading ? (
                <div className="text-center py-4">Loading connections...</div>
              ) : connections.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No connections found. Create your first connection above.
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Subject</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Evaluator</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Relationship</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {connections.map((connection) => (
                        <tr key={connection.Id}>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm font-medium text-gray-900">
                              {connection.SubjectFullName}
                            </div>
                            <div className="text-sm text-gray-500">{connection.SubjectEmail}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm font-medium text-gray-900">
                              {connection.EvaluatorFullName}
                            </div>
                            <div className="text-sm text-gray-500">{connection.EvaluatorEmail}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-indigo-100 text-indigo-800">
                              {connection.Relationship}
                            </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                            <button
                              onClick={() => handleDeleteConnection(connection.Id, connection.SubjectId, connection.EvaluatorId)}
                              className="text-red-600 hover:text-red-900"
                            >
                              Delete
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {/* Information Panel */}
            <div className="bg-gradient-to-r from-indigo-50 to-purple-50 border border-indigo-200 rounded-lg p-6">
              <h3 className="text-lg font-medium text-indigo-900 mb-3 flex items-center">
                <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                How Subject-Evaluator Connections Work
              </h3>
              <div className="text-sm text-indigo-700 space-y-2">
                <p><strong>1. Create Connections:</strong> Link subjects with their evaluators based on workplace relationships (Manager, Peer, Direct Report, etc.).</p>
                <p><strong>2. Searchable Dropdowns:</strong> Use the search boxes to quickly find subjects and evaluators by name, email, or employee ID.</p>
                <p><strong>3. Manage Relationships:</strong> Click &quot;Manage&quot; on any subject or evaluator to view, edit, or delete their relationships.</p>
                <p><strong>4. 360 Feedback:</strong> These connections enable comprehensive 360-degree feedback collection for performance reviews.</p>
              </div>
            </div>
          </div>
        </main>
      </div>

      {/* Logout Confirmation Modal */}
      <LogoutConfirmationModal
        isOpen={showLogoutModal}
        onConfirm={handleLogoutConfirm}
        onCancel={handleLogoutCancel}
      />
    </ProtectedRoute>
  );
}
