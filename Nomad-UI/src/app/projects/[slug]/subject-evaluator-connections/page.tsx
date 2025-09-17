'use client';

import React, { useState, useEffect } from 'react';
import { useParams } from 'next/navigation';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import ProtectedRoute from '@/components/ProtectedRoute';
import toast from 'react-hot-toast';

interface Subject {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  department: string;
  position: string;
}

interface Evaluator {
  id: string;
  evaluatorFirstName: string;
  evaluatorLastName: string;
  evaluatorEmail: string;
  evaluatorDepartment: string;
  evaluatorPosition: string;
}

interface Connection {
  id: string;
  subjectId: string;
  evaluatorId: string;
  relationship: string;
  status: 'pending' | 'approved' | 'rejected';
  createdAt: string;
  subject?: Subject;
  evaluator?: Evaluator;
}

const relationshipTypes = [
  'DirectReport',
  'Manager', 
  'Colleague',
  'Other'
];

export default function SubjectEvaluatorConnections() {
  const params = useParams();
  const { user, logout } = useAuth();
  const projectSlug = params.slug as string;
  
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [evaluators, setEvaluators] = useState<Evaluator[]>([]);
  const [connections, setConnections] = useState<Connection[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [selectedSubject, setSelectedSubject] = useState('');
  const [selectedEvaluator, setSelectedEvaluator] = useState('');
  const [selectedRelationship, setSelectedRelationship] = useState('');

  useEffect(() => {
    loadData();
  }, [projectSlug]);

  const loadData = async () => {
    setIsLoading(true);
    try {
      // Load subjects, evaluators, and existing connections
      // These would be API calls in a real implementation
      setSubjects([]);
      setEvaluators([]);
      setConnections([]);
    } catch (error) {
      toast.error('Failed to load data');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateConnection = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSubject || !selectedEvaluator || !selectedRelationship) {
      toast.error('Please fill all fields');
      return;
    }

    try {
      // API call to create connection
      toast.success('Connection request created successfully');
      setShowCreateForm(false);
      resetForm();
      loadData();
    } catch (error) {
      toast.error('Failed to create connection');
    }
  };

  const handleApproveConnection = async (connectionId: string) => {
    try {
      // API call to approve connection
      toast.success('Connection approved');
      loadData();
    } catch (error) {
      toast.error('Failed to approve connection');
    }
  };

  const handleRejectConnection = async (connectionId: string) => {
    try {
      // API call to reject connection
      toast.success('Connection rejected');
      loadData();
    } catch (error) {
      toast.error('Failed to reject connection');
    }
  };

  const handleDeleteConnection = async (connectionId: string) => {
    if (!confirm('Are you sure you want to delete this connection?')) return;

    try {
      // API call to delete connection
      toast.success('Connection deleted');
      loadData();
    } catch (error) {
      toast.error('Failed to delete connection');
    }
  };

  const resetForm = () => {
    setSelectedSubject('');
    setSelectedEvaluator('');
    setSelectedRelationship('');
  };

  const getStatusBadge = (status: string) => {
    const statusClasses = {
      pending: 'bg-yellow-100 text-yellow-800',
      approved: 'bg-green-100 text-green-800',
      rejected: 'bg-red-100 text-red-800'
    };
    
    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusClasses[status as keyof typeof statusClasses]}`}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </span>
    );
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
                  onClick={logout}
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
                <div className="border-t pt-6 mt-6">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Create New Connection</h3>
                  <form onSubmit={handleCreateConnection} className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700">Subject</label>
                      <select
                        value={selectedSubject}
                        onChange={(e) => setSelectedSubject(e.target.value)}
                        className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                        required
                      >
                        <option value="">Select Subject</option>
                        {subjects.map((subject) => (
                          <option key={subject.id} value={subject.id}>
                            {subject.firstName} {subject.lastName} ({subject.email})
                          </option>
                        ))}
                      </select>
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700">Evaluator</label>
                      <select
                        value={selectedEvaluator}
                        onChange={(e) => setSelectedEvaluator(e.target.value)}
                        className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                        required
                      >
                        <option value="">Select Evaluator</option>
                        {evaluators.map((evaluator) => (
                          <option key={evaluator.id} value={evaluator.id}>
                            {evaluator.evaluatorFirstName} {evaluator.evaluatorLastName} ({evaluator.evaluatorEmail})
                          </option>
                        ))}
                      </select>
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700">Relationship</label>
                      <select
                        value={selectedRelationship}
                        onChange={(e) => setSelectedRelationship(e.target.value)}
                        className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
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
                    
                    <div className="md:col-span-3 flex justify-end space-x-3">
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
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {connections.map((connection) => (
                        <tr key={connection.id}>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm font-medium text-gray-900">
                              {connection.subject?.firstName} {connection.subject?.lastName}
                            </div>
                            <div className="text-sm text-gray-500">{connection.subject?.email}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm font-medium text-gray-900">
                              {connection.evaluator?.evaluatorFirstName} {connection.evaluator?.evaluatorLastName}
                            </div>
                            <div className="text-sm text-gray-500">{connection.evaluator?.evaluatorEmail}</div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {connection.relationship}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            {getStatusBadge(connection.status)}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {new Date(connection.createdAt).toLocaleDateString()}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                            {connection.status === 'pending' && (
                              <>
                                <button
                                  onClick={() => handleApproveConnection(connection.id)}
                                  className="text-green-600 hover:text-green-900 mr-3"
                                >
                                  Approve
                                </button>
                                <button
                                  onClick={() => handleRejectConnection(connection.id)}
                                  className="text-red-600 hover:text-red-900 mr-3"
                                >
                                  Reject
                                </button>
                              </>
                            )}
                            <button
                              onClick={() => handleDeleteConnection(connection.id)}
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
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
              <h3 className="text-lg font-medium text-blue-900 mb-2">How Subject-Evaluator Connections Work</h3>
              <div className="text-sm text-blue-700 space-y-2">
                <p><strong>1. Create Connections:</strong> Link subjects with their evaluators based on workplace relationships.</p>
                <p><strong>2. Admin Approval:</strong> All connections require admin approval before becoming active.</p>
                <p><strong>3. Relationship Types:</strong> DirectReport, Manager, Colleague, or Other - each affects survey questions.</p>
                <p><strong>4. 360 Feedback:</strong> Approved connections enable comprehensive 360-degree feedback collection.</p>
              </div>
            </div>
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
