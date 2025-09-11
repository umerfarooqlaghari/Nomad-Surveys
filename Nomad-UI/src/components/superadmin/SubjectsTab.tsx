'use client';

import React, { useState } from 'react';
import styles from './SubjectsTab.module.css';

export default function SubjectsTab() {
  const [selectedTenant, setSelectedTenant] = useState('');

  // Mock data
  const tenants = [
    { id: 'acme-corp', name: 'Acme Corporation' },
    { id: 'techcorp', name: 'TechCorp Inc.' },
    { id: 'global-solutions', name: 'Global Solutions' },
  ];

  const subjects = [
    {
      id: 1,
      name: 'John Doe',
      email: 'john.doe@acme.com',
      department: 'Engineering',
      position: 'Senior Developer',
      tenant: 'Acme Corporation',
      status: 'Active',
      joinDate: '2024-01-15',
    },
    {
      id: 2,
      name: 'Jane Smith',
      email: 'jane.smith@acme.com',
      department: 'Marketing',
      position: 'Marketing Manager',
      tenant: 'Acme Corporation',
      status: 'Active',
      joinDate: '2024-02-01',
    },
    {
      id: 3,
      name: 'Mike Johnson',
      email: 'mike.johnson@techcorp.com',
      department: 'Sales',
      position: 'Sales Representative',
      tenant: 'TechCorp Inc.',
      status: 'Inactive',
      joinDate: '2024-01-20',
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
    console.log('Downloading template...');
  };

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h2>Subjects (Participants)</h2>
          <p>Manage participants who will take surveys</p>
        </div>
        <div className={styles.importActions}>
          <button
            onClick={downloadTemplate}
            className={styles.downloadButton}
          >
            TMPL Download Template
          </button>
          <label className={styles.importButton}>
            IMPT Import Excel/CSV
            <input
              type="file"
              accept=".xlsx,.xls,.csv"
              onChange={handleFileUpload}
              style={{ display: 'none' }}
            />
          </label>
        </div>
      </div>

      {/* Filters */}
      <div className={styles.formCard}>
        <div className={styles.formGrid}>
          <div className={styles.fieldGroup}>
            <label className={styles.label}>
              Filter by Tenant
            </label>
            <select
              value={selectedTenant}
              onChange={(e) => setSelectedTenant(e.target.value)}
              className={styles.select}
            >
              <option value="">All Tenants</option>
              {tenants.map((tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.name}
                </option>
              ))}
            </select>
          </div>
          <div className={styles.fieldGroup}>
            <label className={styles.label}>
              Search
            </label>
            <input
              type="text"
              placeholder="Search by name or email..."
              className={styles.input}
            />
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
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <span className="text-blue-500">ℹ️</span>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-blue-800">Import Instructions</h3>
            <div className="mt-2 text-sm text-blue-700">
              <ul className="list-disc list-inside space-y-1">
                <li>Download the template to see the required format</li>
                <li>Fill in participant information including name, email, department, and position</li>
                <li>Ensure all required fields are completed</li>
                <li>Upload the completed file using the Import button</li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      {/* Subjects Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
          <h3 className="text-lg font-medium text-gray-900">Participants List</h3>
          <div className="text-sm text-gray-500">
            Total: {subjects.length} participants
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Participant
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Department
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Position
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Tenant
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Join Date
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
              {subjects.map((subject) => (
                <tr key={subject.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="flex-shrink-0 h-10 w-10">
                        <div className="h-10 w-10 rounded-full bg-gray-300 flex items-center justify-center">
                          <span className="text-sm font-medium text-gray-700">
                            {subject.name.split(' ').map(n => n[0]).join('')}
                          </span>
                        </div>
                      </div>
                      <div className="ml-4">
                        <div className="text-sm font-medium text-gray-900">{subject.name}</div>
                        <div className="text-sm text-gray-500">{subject.email}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {subject.department}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {subject.position}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {subject.tenant}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {subject.joinDate}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      subject.status === 'Active' 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {subject.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <button className="text-blue-600 hover:text-blue-900 mr-3">Edit</button>
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
          <button className="bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700 transition-colors">
            Delete Selected
          </button>
          <button className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors">
            Export Selected
          </button>
        </div>
      </div>
    </div>
  );
}
