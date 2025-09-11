'use client';

import React, { useState } from 'react';

export default function EvaluatorsTab() {
  const [selectedTenant, setSelectedTenant] = useState('');

  // Mock data
  const tenants = [
    { id: 'acme-corp', name: 'Acme Corporation' },
    { id: 'techcorp', name: 'TechCorp Inc.' },
    { id: 'global-solutions', name: 'Global Solutions' },
  ];

  const evaluators = [
    {
      id: 1,
      name: 'Dr. Sarah Wilson',
      email: 'sarah.wilson@acme.com',
      department: 'HR',
      position: 'HR Director',
      tenant: 'Acme Corporation',
      status: 'Active',
      evaluationsCount: 15,
      specialization: 'Leadership Assessment',
      joinDate: '2024-01-10',
    },
    {
      id: 2,
      name: 'Prof. Michael Brown',
      email: 'michael.brown@techcorp.com',
      department: 'Management',
      position: 'Senior Manager',
      tenant: 'TechCorp Inc.',
      status: 'Active',
      evaluationsCount: 23,
      specialization: 'Performance Review',
      joinDate: '2024-01-05',
    },
    {
      id: 3,
      name: 'Lisa Anderson',
      email: 'lisa.anderson@global.com',
      department: 'Quality Assurance',
      position: 'QA Lead',
      tenant: 'Global Solutions',
      status: 'Inactive',
      evaluationsCount: 8,
      specialization: 'Technical Skills',
      joinDate: '2024-02-15',
    },
  ];

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      // TODO: Implement file upload logic
      console.log('File selected:', file.name);
    }
  };

  const downloadTemplate = () => {
    // TODO: Implement template download
    console.log('Downloading evaluator template...');
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Evaluators</h2>
          <p className="text-sm text-gray-600">Manage evaluators who will assess participants</p>
        </div>
        <div className="flex space-x-3">
          <button
            onClick={downloadTemplate}
            className="bg-gray-600 text-white px-4 py-2 rounded-md hover:bg-gray-700 transition-colors"
          >
            📥 Download Template
          </button>
          <label className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors cursor-pointer">
            📤 Import Excel/CSV
            <input
              type="file"
              accept=".xlsx,.xls,.csv"
              onChange={handleFileUpload}
              className="hidden"
            />
          </label>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Filter by Tenant
            </label>
            <select
              value={selectedTenant}
              onChange={(e) => setSelectedTenant(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Tenants</option>
              {tenants.map((tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Search
            </label>
            <input
              type="text"
              placeholder="Search by name or email..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Specialization
            </label>
            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">All Specializations</option>
              <option value="leadership">Leadership Assessment</option>
              <option value="performance">Performance Review</option>
              <option value="technical">Technical Skills</option>
              <option value="behavioral">Behavioral Analysis</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">All Status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>
        </div>
      </div>

      {/* Import Instructions */}
      <div className="bg-purple-50 border border-purple-200 rounded-lg p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <span className="text-purple-500">ℹ️</span>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-purple-800">Evaluator Import Instructions</h3>
            <div className="mt-2 text-sm text-purple-700">
              <ul className="list-disc list-inside space-y-1">
                <li>Download the evaluator template to see the required format</li>
                <li>Include evaluator credentials, specialization, and experience level</li>
                <li>Specify which tenant the evaluator belongs to</li>
                <li>Ensure all required fields are completed before upload</li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">👨‍💼</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Evaluators</p>
              <p className="text-2xl font-semibold text-gray-900">{evaluators.length}</p>
            </div>
          </div>
        </div>
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">✅</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Active Evaluators</p>
              <p className="text-2xl font-semibold text-gray-900">
                {evaluators.filter(e => e.status === 'Active').length}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">📊</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Evaluations</p>
              <p className="text-2xl font-semibold text-gray-900">
                {evaluators.reduce((sum, e) => sum + e.evaluationsCount, 0)}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">📈</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Avg. Evaluations</p>
              <p className="text-2xl font-semibold text-gray-900">
                {Math.round(evaluators.reduce((sum, e) => sum + e.evaluationsCount, 0) / evaluators.length)}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Evaluators Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
          <h3 className="text-lg font-medium text-gray-900">Evaluators List</h3>
          <div className="text-sm text-gray-500">
            Total: {evaluators.length} evaluators
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Evaluator
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Department
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Specialization
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Tenant
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Evaluations
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {evaluators.map((evaluator) => (
                <tr key={evaluator.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="flex-shrink-0 h-10 w-10">
                        <div className="h-10 w-10 rounded-full bg-purple-300 flex items-center justify-center">
                          <span className="text-sm font-medium text-purple-700">
                            {evaluator.name.split(' ').map(n => n[0]).join('')}
                          </span>
                        </div>
                      </div>
                      <div className="ml-4">
                        <div className="text-sm font-medium text-gray-900">{evaluator.name}</div>
                        <div className="text-sm text-gray-500">{evaluator.email}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{evaluator.department}</div>
                    <div className="text-sm text-gray-500">{evaluator.position}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {evaluator.specialization}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {evaluator.tenant}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded-full text-xs font-medium">
                      {evaluator.evaluationsCount}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      evaluator.status === 'Active' 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {evaluator.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <button className="text-blue-600 hover:text-blue-900 mr-3">Edit</button>
                    <button className="text-green-600 hover:text-green-900 mr-3">Assign</button>
                    <button className="text-red-600 hover:text-red-900">Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Bulk Actions */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Bulk Actions</h3>
        <div className="flex space-x-4">
          <button className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700 transition-colors">
            Activate Selected
          </button>
          <button className="bg-yellow-600 text-white px-4 py-2 rounded-md hover:bg-yellow-700 transition-colors">
            Deactivate Selected
          </button>
          <button className="bg-purple-600 text-white px-4 py-2 rounded-md hover:bg-purple-700 transition-colors">
            Assign to Survey
          </button>
          <button className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors">
            Export Selected
          </button>
        </div>
      </div>
    </div>
  );
}
