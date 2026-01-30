/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { subjectService, SubjectListResponse, EvaluatorRelationship } from '@/services/subjectService';
import { evaluatorService, EvaluatorListResponse, SubjectRelationship } from '@/services/evaluatorService';
import { employeeService, EmployeeListResponse } from '@/services/employeeService';
import ManageRelationshipsModal from '@/components/modals/ManageRelationshipsModal';
import ViewRelationshipsModal from '@/components/modals/ViewRelationshipsModal';
import { EmailingListItem } from '@/services/evaluatorService';
import emailService from '@/services/emailService';

interface ProjectParticipantsTabProps {
  projectSlug: string;
}

type SubTabType = 'evaluators' | 'subjects' | 'emailing';

export default function ProjectParticipantsTab({ projectSlug }: ProjectParticipantsTabProps) {
  const { token } = useAuth();
  const [activeSubTab, setActiveSubTab] = useState<SubTabType>('evaluators');

  // Evaluators state
  const [evaluators, setEvaluators] = useState<EvaluatorListResponse[]>([]);
  const [evaluatorsLoading, setEvaluatorsLoading] = useState(false);
  const [showAddEvaluatorForm, setShowAddEvaluatorForm] = useState(false);
  const [selectedEvaluatorEmployeeIds, setSelectedEvaluatorEmployeeIds] = useState<string[]>([]);
  const [evaluatorSearchTerm, setEvaluatorSearchTerm] = useState('');
  const [evaluatorFilterStatus, setEvaluatorFilterStatus] = useState<'all' | 'active' | 'inactive'>('all');
  const [isImportingEvaluators, setIsImportingEvaluators] = useState(false);
  const [showEvaluatorRelationshipsModal, setShowEvaluatorRelationshipsModal] = useState(false);
  const [showEvaluatorViewModal, setShowEvaluatorViewModal] = useState(false);
  const [selectedEvaluator, setSelectedEvaluator] = useState<{ id: string; name: string } | null>(null);
  const [selectedEvaluatorForView, setSelectedEvaluatorForView] = useState<{ id: string; name: string } | null>(null);

  // Subjects state
  const [subjects, setSubjects] = useState<SubjectListResponse[]>([]);
  const [subjectsLoading, setSubjectsLoading] = useState(false);
  const [showAddSubjectForm, setShowAddSubjectForm] = useState(false);
  const [selectedSubjectEmployeeIds, setSelectedSubjectEmployeeIds] = useState<string[]>([]);
  const [subjectSearchTerm, setSubjectSearchTerm] = useState('');
  const [subjectFilterStatus, setSubjectFilterStatus] = useState<'all' | 'active' | 'inactive'>('all');
  const [isImportingSubjects, setIsImportingSubjects] = useState(false);
  const [showSubjectRelationshipsModal, setShowSubjectRelationshipsModal] = useState(false);
  const [showSubjectViewModal, setShowSubjectViewModal] = useState(false);
  const [selectedSubject, setSelectedSubject] = useState<{ id: string; name: string } | null>(null);
  const [selectedSubjectForView, setSelectedSubjectForView] = useState<{ id: string; name: string } | null>(null);

  // Emailing state
  const [emailingList, setEmailingList] = useState<EmailingListItem[]>([]);
  const [emailingLoading, setEmailingLoading] = useState(false);
  const [emailingSearchTerm, setEmailingSearchTerm] = useState('');
  const [selectedEmailingItems, setSelectedEmailingItems] = useState<string[]>([]); // Array of SurveyId|EvaluatorId
  const [isSendingEmails, setIsSendingEmails] = useState(false);

  // Shared state
  const [employees, setEmployees] = useState<EmployeeListResponse[]>([]);

  useEffect(() => {
    if (activeSubTab === 'evaluators' && evaluators.length === 0) {
      loadEvaluators();
    } else if (activeSubTab === 'subjects' && subjects.length === 0) {
      loadSubjects();
    } else if (activeSubTab === 'emailing' && emailingList.length === 0) {
      loadEmailingList();
    }
    if (employees.length === 0) {
      loadEmployees();
    }
  }, [projectSlug, activeSubTab]);

  // Evaluators functions
  const loadEvaluators = async () => {
    if (!token) return;
    setEvaluatorsLoading(true);
    try {
      const response = await evaluatorService.getEvaluators(projectSlug, token);
      if (response.error) {
        setEvaluators([]);
        toast.error(response.error);
      } else {
        setEvaluators(response.data || []);
      }
    } catch (error) {
      console.error('Error loading evaluators:', error);
      toast.error('Failed to load evaluators');
      setEvaluators([]);
    } finally {
      setEvaluatorsLoading(false);
    }
  };

  const handleEvaluatorEmployeeSelect = (employeeId: string) => {
    setSelectedEvaluatorEmployeeIds(prev =>
      prev.includes(employeeId) ? prev.filter(id => id !== employeeId) : [...prev, employeeId]
    );
  };

  const handleEvaluatorSelectAll = () => {
    const available = getAvailableEmployeesForEvaluators();
    setSelectedEvaluatorEmployeeIds(
      selectedEvaluatorEmployeeIds.length === available.length
        ? []
        : available.map(emp => emp.EmployeeId)
    );
  };

  const getAvailableEmployeesForEvaluators = () => {
    const evaluatorEmployeeIds = evaluators.map(e => e.EmployeeIdString);
    return employees.filter(emp => !evaluatorEmployeeIds.includes(emp.EmployeeId));
  };

  const handleEvaluatorSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedEvaluatorEmployeeIds.length === 0) {
      toast.error('Please select at least one employee');
      return;
    }
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    setEvaluatorsLoading(true);
    try {
      const evaluatorsToCreate = selectedEvaluatorEmployeeIds.map(employeeId => ({
        EmployeeId: employeeId,
        SubjectRelationships: [] as SubjectRelationship[]
      }));

      const response = await evaluatorService.bulkCreateEvaluators(
        projectSlug,
        { Evaluators: evaluatorsToCreate },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully created ${response.data.SuccessfullyCreated} evaluator(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        setSelectedEvaluatorEmployeeIds([]);
        setShowAddEvaluatorForm(false);
        loadEvaluators();
      }
    } catch (error) {
      console.error('Error creating evaluators:', error);
      toast.error('Failed to create evaluators');
    } finally {
      setEvaluatorsLoading(false);
    }
  };

  const handleEvaluatorBulkImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !token) return;

    setIsImportingEvaluators(true);
    try {
      const text = await file.text();
      const parseResult = evaluatorService.parseCSV(text);

      if (parseResult.errors.length > 0) {
        parseResult.errors.forEach(err => toast.error(err));
        if (parseResult.evaluators.length === 0) {
          setIsImportingEvaluators(false);
          return;
        }
      }

      const response = await evaluatorService.bulkCreateEvaluators(
        projectSlug,
        { Evaluators: parseResult.evaluators },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully imported ${response.data.SuccessfullyCreated} evaluator(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        loadEvaluators();
      }
    } catch (error) {
      console.error('Error importing evaluators:', error);
      toast.error('Failed to import evaluators');
    } finally {
      setIsImportingEvaluators(false);
      e.target.value = '';
    }
  };

  const downloadEvaluatorTemplate = () => {
    const csvContent = `EmployeeId,SubjectEmployeeId,Relationship
EMP001,EMP002,Manager
EMP001,EMP003,Peer`;
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'evaluators_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const handleEvaluatorDelete = async (evaluatorId: string) => {
    if (!confirm('Are you sure you want to delete this evaluator?')) return;
    if (!token) return;

    try {
      const response = await evaluatorService.deleteEvaluator(projectSlug, evaluatorId, token);
      if (response.error) {
        toast.error(response.error);
      } else {
        toast.success('Evaluator deleted successfully');
        loadEvaluators();
      }
    } catch (error) {
      console.error('Error deleting evaluator:', error);
      toast.error('Failed to delete evaluator');
    }
  };

  // Subjects functions
  const loadSubjects = async () => {
    if (!token) return;
    setSubjectsLoading(true);
    try {
      const response = await subjectService.getSubjects(projectSlug, token);
      if (response.error) {
        setSubjects([]);
        toast.error(response.error);
      } else {
        setSubjects(response.data || []);
      }
    } catch (error) {
      console.error('Error loading subjects:', error);
      toast.error('Failed to load subjects');
      setSubjects([]);
    } finally {
      setSubjectsLoading(false);
    }
  };

  const handleSubjectEmployeeSelect = (employeeId: string) => {
    setSelectedSubjectEmployeeIds(prev =>
      prev.includes(employeeId) ? prev.filter(id => id !== employeeId) : [...prev, employeeId]
    );
  };

  const handleSubjectSelectAll = () => {
    const available = getAvailableEmployeesForSubjects();
    setSelectedSubjectEmployeeIds(
      selectedSubjectEmployeeIds.length === available.length
        ? []
        : available.map(emp => emp.EmployeeId)
    );
  };

  const getAvailableEmployeesForSubjects = () => {
    const subjectEmployeeIds = subjects.map(s => s.EmployeeIdString);
    return employees.filter(emp => !subjectEmployeeIds.includes(emp.EmployeeId));
  };

  const handleSubjectSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedSubjectEmployeeIds.length === 0) {
      toast.error('Please select at least one employee');
      return;
    }
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    setSubjectsLoading(true);
    try {
      const subjectsToCreate = selectedSubjectEmployeeIds.map(employeeId => ({
        EmployeeId: employeeId,
        EvaluatorRelationships: [] as EvaluatorRelationship[]
      }));

      const response = await subjectService.bulkCreateSubjects(
        projectSlug,
        { Subjects: subjectsToCreate },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully created ${response.data.SuccessfullyCreated} subject(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        setSelectedSubjectEmployeeIds([]);
        setShowAddSubjectForm(false);
        loadSubjects();
      }
    } catch (error) {
      console.error('Error creating subjects:', error);
      toast.error('Failed to create subjects');
    } finally {
      setSubjectsLoading(false);
    }
  };

  const handleSubjectBulkImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !token) return;

    setIsImportingSubjects(true);
    try {
      const text = await file.text();
      const parseResult = subjectService.parseCSV(text);

      if (parseResult.errors.length > 0) {
        parseResult.errors.forEach(err => toast.error(err));
        if (parseResult.subjects.length === 0) {
          setIsImportingSubjects(false);
          return;
        }
      }

      const response = await subjectService.bulkCreateSubjects(
        projectSlug,
        { Subjects: parseResult.subjects },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully imported ${response.data.SuccessfullyCreated} subject(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        loadSubjects();
      }
    } catch (error) {
      console.error('Error importing subjects:', error);
      toast.error('Failed to import subjects');
    } finally {
      setIsImportingSubjects(false);
      e.target.value = '';
    }
  };

  const downloadSubjectTemplate = () => {
    const csvContent = `EmployeeId,EvaluatorEmployeeId,Relationship
EMP001,EMP002,Manager
EMP001,EMP003,Peer`;
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'subjects_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const handleSubjectDelete = async (subjectId: string) => {
    if (!confirm('Are you sure you want to delete this subject?')) return;
    if (!token) return;

    try {
      const response = await subjectService.deleteSubject(projectSlug, subjectId, token);
      if (response.error) {
        toast.error(response.error);
      } else {
        toast.success('Subject deleted successfully');
        loadSubjects();
      }
    } catch (error) {
      console.error('Error deleting subject:', error);
      toast.error('Failed to delete subject');
    }
  };

  // Emailing functions
  const loadEmailingList = async () => {
    if (!token) return;
    setEmailingLoading(true);
    try {
      const response = await evaluatorService.getEmailingList(projectSlug, token);
      if (response.error) {
        setEmailingList([]);
        toast.error(response.error);
      } else {
        setEmailingList(response.data || []);
      }
    } catch (error) {
      console.error('Error loading emailing list:', error);
      toast.error('Failed to load emailing list');
      setEmailingList([]);
    } finally {
      setEmailingLoading(false);
    }
  };

  const handleEmailingItemSelect = (surveyId: string, evaluatorId: string) => {
    const key = `${surveyId}|${evaluatorId}`;
    setSelectedEmailingItems(prev =>
      prev.includes(key) ? prev.filter(id => id !== key) : [...prev, key]
    );
  };

  const handleEmailingSelectAll = (itemsToSelect: EmailingListItem[]) => {
    if (selectedEmailingItems.length === itemsToSelect.length) {
      setSelectedEmailingItems([]);
    } else {
      setSelectedEmailingItems(itemsToSelect.map(item => `${item.SurveyId}|${item.EvaluatorId}`));
    }
  };

  const handleSendBulkReminders = async () => {
    if (selectedEmailingItems.length === 0 || !token) return;

    setIsSendingEmails(true);
    const loadingToast = toast.loading('Sending reminders...');
    try {
      const surveyEvaluatorSurveyIds = emailingList
        .filter(item => selectedEmailingItems.includes(`${item.SurveyId}|${item.EvaluatorId}`))
        .flatMap(item => item.SubjectEvaluatorSurveyIds);

      const response = await emailService.sendBulkReminders(projectSlug, surveyEvaluatorSurveyIds, token);
      if (response.success) {
        toast.success(response.message);
        loadEmailingList();
        setSelectedEmailingItems([]);
      } else {
        toast.error(response.error || 'Failed to send reminders');
      }
    } catch (error) {
      console.error('Error sending bulk reminders:', error);
      toast.error('An unexpected error occurred');
    } finally {
      setIsSendingEmails(false);
      toast.dismiss(loadingToast);
    }
  };

  const handleSendBulkAssignments = async () => {
    if (selectedEmailingItems.length === 0 || !token) return;

    setIsSendingEmails(true);
    const loadingToast = toast.loading('Sending assignments...');
    try {
      const surveyEvaluatorSurveyIds = emailingList
        .filter(item => selectedEmailingItems.includes(`${item.SurveyId}|${item.EvaluatorId}`))
        .flatMap(item => item.SubjectEvaluatorSurveyIds);

      const response = await emailService.sendBulkAssignments(projectSlug, surveyEvaluatorSurveyIds, token);
      if (response.success) {
        toast.success(response.message);
        loadEmailingList();
        setSelectedEmailingItems([]);
      } else {
        toast.error(response.error || 'Failed to send assignments');
      }
    } catch (error) {
      console.error('Error sending assignments:', error);
      toast.error('An unexpected error occurred');
    } finally {
      setIsSendingEmails(false);
      toast.dismiss(loadingToast);
    }
  };

  // Shared functions
  const loadEmployees = async () => {
    if (!token) return;
    try {
      const response = await employeeService.getEmployees(projectSlug, token);
      if (response.error) {
        console.error('Error loading employees:', response.error);
      } else if (response.data) {
        setEmployees(response.data);
      }
    } catch (error) {
      console.error('Error loading employees:', error);
    }
  };

  // Filter functions
  const filteredEvaluators = evaluators.filter(evaluator => {
    const matchesSearch = evaluatorSearchTerm === '' ||
      evaluator.FullName.toLowerCase().includes(evaluatorSearchTerm.toLowerCase()) ||
      evaluator.Email.toLowerCase().includes(evaluatorSearchTerm.toLowerCase()) ||
      evaluator.EmployeeIdString.toLowerCase().includes(evaluatorSearchTerm.toLowerCase());
    const matchesStatus = evaluatorFilterStatus === 'all' ||
      (evaluatorFilterStatus === 'active' && evaluator.IsActive) ||
      (evaluatorFilterStatus === 'inactive' && !evaluator.IsActive);
    return matchesSearch && matchesStatus;
  });

  const filteredSubjects = subjects.filter(subject => {
    const matchesSearch = subjectSearchTerm === '' ||
      subject.FullName.toLowerCase().includes(subjectSearchTerm.toLowerCase()) ||
      subject.Email.toLowerCase().includes(subjectSearchTerm.toLowerCase()) ||
      subject.EmployeeIdString.toLowerCase().includes(subjectSearchTerm.toLowerCase());
    const matchesStatus = subjectFilterStatus === 'all' ||
      (subjectFilterStatus === 'active' && subject.IsActive) ||
      (subjectFilterStatus === 'inactive' && !subject.IsActive);
    return matchesSearch && matchesStatus;
  });

  const filteredAvailableEmployeesForEvaluators = getAvailableEmployeesForEvaluators().filter(emp =>
    evaluatorSearchTerm === '' ||
    emp.FullName.toLowerCase().includes(evaluatorSearchTerm.toLowerCase()) ||
    emp.Email.toLowerCase().includes(evaluatorSearchTerm.toLowerCase()) ||
    emp.EmployeeId.toLowerCase().includes(evaluatorSearchTerm.toLowerCase())
  );

  const filteredAvailableEmployeesForSubjects = getAvailableEmployeesForSubjects().filter(emp =>
    subjectSearchTerm === '' ||
    emp.FullName.toLowerCase().includes(subjectSearchTerm.toLowerCase()) ||
    emp.Email.toLowerCase().includes(subjectSearchTerm.toLowerCase()) ||
    emp.EmployeeId.toLowerCase().includes(subjectSearchTerm.toLowerCase())
  );

  const filteredEmailingList = emailingList.filter(item =>
    emailingSearchTerm === '' ||
    item.SurveyName.toLowerCase().includes(emailingSearchTerm.toLowerCase()) ||
    item.EvaluatorName.toLowerCase().includes(emailingSearchTerm.toLowerCase()) ||
    item.EvaluatorEmail.toLowerCase().includes(emailingSearchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      {/* Sub-tabs */}
      <div className="bg-white shadow-md rounded-lg">
        <div className="border-b border-gray-200">
          <nav className="flex -mb-px">
            <button
              onClick={() => setActiveSubTab('evaluators')}
              className={`${activeSubTab === 'evaluators'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-4 px-6 border-b-2 font-medium text-sm`}
            >
              Evaluators
            </button>
            <button
              onClick={() => setActiveSubTab('subjects')}
              className={`${activeSubTab === 'subjects'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-4 px-6 border-b-2 font-medium text-sm`}
            >
              Subjects
            </button>
            <button
              onClick={() => setActiveSubTab('emailing')}
              className={`${activeSubTab === 'emailing'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-4 px-6 border-b-2 font-medium text-sm`}
            >
              Emailing
            </button>
          </nav>
        </div>

        <div className="p-6">
          {/* Evaluators Tab Content */}
          {activeSubTab === 'evaluators' && (
            <>
              <div className="flex justify-between items-center mb-6">
                <h2 className="text-2xl font-bold text-gray-900">Evaluators</h2>
                <div className="flex gap-2">
                  {/* <button
                    onClick={downloadEvaluatorTemplate}
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                  >
                    Download CSV Template
                  </button>
                  <label
                    htmlFor="evaluator-csv-upload"
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer inline-block"
                  >
                    {isImportingEvaluators ? 'Importing...' : 'Bulk Import CSV'}
                    <input
                      id="evaluator-csv-upload"
                      type="file"
                      accept=".csv"
                      onChange={handleEvaluatorBulkImport}
                      className="hidden"
                      disabled={isImportingEvaluators}
                    />
                  </label> */}
                  <button
                    onClick={() => setShowAddEvaluatorForm(!showAddEvaluatorForm)}
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                  >
                    {showAddEvaluatorForm ? 'Cancel' : 'Add Evaluators'}
                  </button>
                </div>
              </div>

              {showAddEvaluatorForm && (
                <div className="border-t pt-6 mt-6">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">
                    Select Employees to Add as Evaluators
                  </h3>
                  <form onSubmit={handleEvaluatorSubmit} className="bg-gray-50 p-6 rounded-lg">
                    <div className="mb-4">
                      <input
                        type="text"
                        placeholder="Search employees..."
                        value={evaluatorSearchTerm}
                        onChange={(e) => setEvaluatorSearchTerm(e.target.value)}
                        className="w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white"
                      />
                    </div>
                    <div className="mb-4 flex items-center gap-4">
                      <label className="flex items-center">
                        <input
                          type="checkbox"
                          checked={selectedEvaluatorEmployeeIds.length === filteredAvailableEmployeesForEvaluators.length && filteredAvailableEmployeesForEvaluators.length > 0}
                          onChange={handleEvaluatorSelectAll}
                          className="mr-2"
                        />
                        <span className="text-sm font-medium text-gray-700">Select All ({filteredAvailableEmployeesForEvaluators.length})</span>
                      </label>
                      <span className="text-sm text-gray-600">
                        {selectedEvaluatorEmployeeIds.length} selected
                      </span>
                    </div>
                    <div className="max-h-96 overflow-y-auto border border-gray-300 rounded-md bg-white">
                      {filteredAvailableEmployeesForEvaluators.length === 0 ? (
                        <div className="p-4 text-center text-gray-500">No available employees found</div>
                      ) : (
                        filteredAvailableEmployeesForEvaluators.map(employee => (
                          <div
                            key={employee.Id}
                            className="flex items-center p-3 hover:bg-gray-50 border-b border-gray-200 cursor-pointer"
                            onClick={() => handleEvaluatorEmployeeSelect(employee.EmployeeId)}
                          >
                            <input
                              type="checkbox"
                              checked={selectedEvaluatorEmployeeIds.includes(employee.EmployeeId)}
                              onChange={() => { }}
                              className="mr-3 cursor-pointer"
                              onClick={(e) => e.stopPropagation()}
                            />
                            <div className="flex-1">
                              <div className="font-medium text-gray-900">
                                {employee.FullName} ({employee.EmployeeId})
                              </div>
                              <div className="text-sm text-gray-600">
                                {employee.Email} {employee.Designation && `• ${employee.Designation}`}
                              </div>
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                    <div className="mt-4 flex justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => {
                          setShowAddEvaluatorForm(false);
                          setSelectedEvaluatorEmployeeIds([]);
                          setEvaluatorSearchTerm('');
                        }}
                        className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        disabled={evaluatorsLoading || selectedEvaluatorEmployeeIds.length === 0}
                        className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-700 disabled:bg-gray-300"
                      >
                        {evaluatorsLoading ? 'Creating...' : `Create ${selectedEvaluatorEmployeeIds.length} Evaluator(s)`}
                      </button>
                    </div>
                  </form>
                </div>
              )}

              <div className="mb-6 flex gap-4">
                <input
                  type="text"
                  placeholder="Search evaluators by name, email, or employee ID..."
                  value={evaluatorSearchTerm}
                  onChange={(e) => setEvaluatorSearchTerm(e.target.value)}
                  className="flex-1 border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
                />
                <select
                  value={evaluatorFilterStatus}
                  onChange={(e) => setEvaluatorFilterStatus(e.target.value as 'all' | 'active' | 'inactive')}
                  className="border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
                >
                  <option value="all">All Status</option>
                  <option value="active">Active</option>
                  <option value="inactive">Inactive</option>
                </select>
              </div>

              {evaluatorsLoading ? (
                <div className="text-center py-8">
                  <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                  <p className="mt-2 text-gray-600">Loading evaluators...</p>
                </div>
              ) : filteredEvaluators.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  {evaluatorSearchTerm || evaluatorFilterStatus !== 'all' ? 'No evaluators match your filters' : 'No evaluators found. Add some evaluators to get started.'}
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Employee ID</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Relationships</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Evaluations Completed</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Evaluations Received</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Is Subject</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {filteredEvaluators.map((evaluator) => (
                        <tr key={evaluator.Id} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {evaluator.EmployeeIdString}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {evaluator.FullName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            {evaluator.Email}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            <button
                              onClick={() => {
                                setSelectedEvaluatorForView({ id: evaluator.Id, name: evaluator.FullName });
                                setShowEvaluatorViewModal(true);
                              }}
                              className="text-blue-600 hover:text-blue-900 font-medium"
                            >
                              View ({evaluator.SubjectCount || 0})
                            </button>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            {evaluator.EvaluationsCompleted || '0/0'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            {evaluator.EvaluationsReceived || '-'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 text-center">
                            {evaluator.IsSubject ? (
                              <span className="text-green-600 font-bold" title="Also a Subject">✓</span>
                            ) : '-'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                            <button
                              onClick={() => {
                                setSelectedEvaluator({ id: evaluator.Id, name: evaluator.FullName });
                                setShowEvaluatorRelationshipsModal(true);
                              }}
                              className="text-indigo-600 hover:text-indigo-900 mr-4"
                            >
                              Manage
                            </button>
                            <button
                              onClick={() => handleEvaluatorDelete(evaluator.Id)}
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

              <div className="mt-4 text-sm text-gray-600">
                Showing {filteredEvaluators.length} of {evaluators.length} evaluators
              </div>
            </>
          )}

          {/* Subjects Tab Content */}
          {activeSubTab === 'subjects' && (
            <>
              <div className="flex justify-between items-center mb-6">
                <h2 className="text-2xl font-bold text-gray-900">Subjects</h2>
                <div className="flex gap-2">
                  {/* <button
                    onClick={downloadSubjectTemplate}
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                  >
                    Download CSV Template
                  </button>
                  <label
                    htmlFor="subject-csv-upload"
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer inline-block"
                  >
                    {isImportingSubjects ? 'Importing...' : 'Bulk Import CSV'}
                    <input
                      id="subject-csv-upload"
                      type="file"
                      accept=".csv"
                      onChange={handleSubjectBulkImport}
                      className="hidden"
                      disabled={isImportingSubjects}
                    />
                  </label>*/}
                  <button
                    onClick={() => setShowAddSubjectForm(!showAddSubjectForm)}
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                  >
                    {showAddSubjectForm ? 'Cancel' : 'Add Subjects'}
                  </button>
                </div>
              </div>

              {showAddSubjectForm && (
                <div className="border-t pt-6 mt-6">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">
                    Select Employees to Add as Subjects
                  </h3>
                  <form onSubmit={handleSubjectSubmit} className="bg-gray-50 p-6 rounded-lg">
                    <div className="mb-4">
                      <input
                        type="text"
                        placeholder="Search employees..."
                        value={subjectSearchTerm}
                        onChange={(e) => setSubjectSearchTerm(e.target.value)}
                        className="w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white"
                      />
                    </div>
                    <div className="mb-4 flex items-center gap-4">
                      <label className="flex items-center">
                        <input
                          type="checkbox"
                          checked={selectedSubjectEmployeeIds.length === filteredAvailableEmployeesForSubjects.length && filteredAvailableEmployeesForSubjects.length > 0}
                          onChange={handleSubjectSelectAll}
                          className="mr-2"
                        />
                        <span className="text-sm font-medium text-gray-700">Select All ({filteredAvailableEmployeesForSubjects.length})</span>
                      </label>
                      <span className="text-sm text-gray-600">
                        {selectedSubjectEmployeeIds.length} selected
                      </span>
                    </div>
                    <div className="max-h-96 overflow-y-auto border border-gray-300 rounded-md bg-white">
                      {filteredAvailableEmployeesForSubjects.length === 0 ? (
                        <div className="p-4 text-center text-gray-500">No available employees found</div>
                      ) : (
                        filteredAvailableEmployeesForSubjects.map(employee => (
                          <div
                            key={employee.Id}
                            className="flex items-center p-3 hover:bg-gray-50 border-b border-gray-200 cursor-pointer"
                            onClick={() => handleSubjectEmployeeSelect(employee.EmployeeId)}
                          >
                            <input
                              type="checkbox"
                              checked={selectedSubjectEmployeeIds.includes(employee.EmployeeId)}
                              onChange={() => { }}
                              className="mr-3 cursor-pointer"
                              onClick={(e) => e.stopPropagation()}
                            />
                            <div className="flex-1">
                              <div className="font-medium text-gray-900">
                                {employee.FullName} ({employee.EmployeeId})
                              </div>
                              <div className="text-sm text-gray-600">
                                {employee.Email} {employee.Designation && `• ${employee.Designation}`}
                              </div>
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                    <div className="mt-4 flex justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => {
                          setShowAddSubjectForm(false);
                          setSelectedSubjectEmployeeIds([]);
                          setSubjectSearchTerm('');
                        }}
                        className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        disabled={subjectsLoading || selectedSubjectEmployeeIds.length === 0}
                        className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-700 disabled:bg-gray-300"
                      >
                        {subjectsLoading ? 'Creating...' : `Create ${selectedSubjectEmployeeIds.length} Subject(s)`}
                      </button>
                    </div>
                  </form>
                </div>
              )}

              <div className="mb-6 flex gap-4">
                <input
                  type="text"
                  placeholder="Search subjects by name, email, or employee ID..."
                  value={subjectSearchTerm}
                  onChange={(e) => setSubjectSearchTerm(e.target.value)}
                  className="flex-1 border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
                />
                <select
                  value={subjectFilterStatus}
                  onChange={(e) => setSubjectFilterStatus(e.target.value as 'all' | 'active' | 'inactive')}
                  className="border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
                >
                  <option value="all">All Status</option>
                  <option value="active">Active</option>
                  <option value="inactive">Inactive</option>
                </select>
              </div>

              {subjectsLoading ? (
                <div className="text-center py-8">
                  <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                  <p className="mt-2 text-gray-600">Loading subjects...</p>
                </div>
              ) : filteredSubjects.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  {subjectSearchTerm || subjectFilterStatus !== 'all' ? 'No subjects match your filters' : 'No subjects found. Add some subjects to get started.'}
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Employee ID</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Relationships</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Evaluations Received</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Evaluations Completed</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Is Evaluator</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {filteredSubjects.map((subject) => (
                        <tr key={subject.Id} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {subject.EmployeeIdString}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {subject.FullName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            {subject.Email}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            <button
                              onClick={() => {
                                setSelectedSubjectForView({ id: subject.Id, name: subject.FullName });
                                setShowSubjectViewModal(true);
                              }}
                              className="text-blue-600 hover:text-blue-900 font-medium"
                            >
                              View ({subject.EvaluatorCount || 0})
                            </button>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            {subject.EvaluationsReceived || '0/0'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                            {subject.EvaluationsCompleted || '-'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 text-center">
                            {subject.IsEvaluator ? (
                              <span className="text-green-600 font-bold" title="Also an Evaluator">✓</span>
                            ) : '-'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                            <button
                              onClick={() => {
                                setSelectedSubject({ id: subject.Id, name: subject.FullName });
                                setShowSubjectRelationshipsModal(true);
                              }}
                              className="text-indigo-600 hover:text-indigo-900 mr-4"
                            >
                              Manage
                            </button>
                            <button
                              onClick={() => handleSubjectDelete(subject.Id)}
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

              <div className="mt-4 text-sm text-gray-600">
                Showing {filteredSubjects.length} of {subjects.length} subjects
              </div>
            </>
          )}

          {activeSubTab === 'emailing' && (
            <>
              <div className="flex justify-between items-center mb-6">
                <div className="flex items-center gap-4">
                  <h2 className="text-2xl font-bold text-gray-900">Emailing</h2>
                  <button
                    onClick={loadEmailingList}
                    disabled={emailingLoading}
                    className="p-1 hover:bg-gray-100 rounded-full transition-colors disabled:opacity-50"
                    title="Refresh list"
                  >
                    <svg className={`w-5 h-5 text-gray-600 ${emailingLoading ? 'animate-spin' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                  </button>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={handleSendBulkReminders}
                    disabled={selectedEmailingItems.length === 0 || isSendingEmails}
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium disabled:bg-gray-300"
                  >
                    Send Reminder email
                  </button>
                  <button
                    onClick={handleSendBulkAssignments}
                    disabled={selectedEmailingItems.length === 0 || isSendingEmails}
                    className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium disabled:bg-gray-300"
                  >
                    Send assignment email
                  </button>
                </div>
              </div>

              <div className="mb-6">
                <input
                  type="text"
                  placeholder="Search by survey name, evaluator name or email..."
                  value={emailingSearchTerm}
                  onChange={(e) => setEmailingSearchTerm(e.target.value)}
                  className="w-full border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
                />
              </div>

              {emailingLoading ? (
                <div className="text-center py-8">
                  <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                  <p className="mt-2 text-gray-600">Loading emailing list...</p>
                </div>
              ) : filteredEmailingList.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  {emailingSearchTerm ? 'No items match your search' : 'No survey assignments found.'}
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-10">
                          <div
                            className="flex items-center justify-center cursor-pointer"
                            onClick={() => handleEmailingSelectAll(filteredEmailingList)}
                          >
                            <div className={`w-5 h-5 rounded border ${selectedEmailingItems.length === filteredEmailingList.length && filteredEmailingList.length > 0
                              ? 'bg-blue-600 border-blue-600'
                              : 'border-gray-300 bg-white'
                              } flex items-center justify-center transition-colors`}>
                              {selectedEmailingItems.length === filteredEmailingList.length && filteredEmailingList.length > 0 && (
                                <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                                </svg>
                              )}
                            </div>
                          </div>
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Survey Name</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Evaluator Name</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Number of Subjects</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Reminder email time</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {filteredEmailingList.map((item) => {
                        const isSelected = selectedEmailingItems.includes(`${item.SurveyId}|${item.EvaluatorId}`);
                        return (
                          <tr
                            key={`${item.SurveyId}-${item.EvaluatorId}`}
                            className={`transition-colors cursor-pointer ${isSelected ? 'bg-blue-50' : 'hover:bg-gray-50'}`}
                            onClick={() => handleEmailingItemSelect(item.SurveyId, item.EvaluatorId)}
                          >
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                              <div className="flex items-center justify-center">
                                <div className={`w-5 h-5 rounded border ${isSelected ? 'bg-blue-600 border-blue-600' : 'border-gray-300 bg-white'
                                  } flex items-center justify-center transition-colors`}>
                                  {isSelected && (
                                    <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                                    </svg>
                                  )}
                                </div>
                              </div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 border-b border-transparent">
                              {item.SurveyName}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                              {item.EvaluatorName}
                              <div className="text-xs text-gray-500 font-normal">{item.EvaluatorEmail}</div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600" onClick={(e) => e.stopPropagation()}>
                              <div className="group relative inline-block cursor-help border-b border-dotted border-gray-400">
                                {item.SubjectCount}
                                <div className="invisible group-hover:visible absolute z-[9999] w-64 p-3 mt-2 text-sm bg-gray-900 text-white rounded-lg shadow-xl -left-20 opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none">
                                  <p className="font-semibold mb-2 border-b border-gray-700 pb-1">Assigned Subjects:</p>
                                  <ul className="list-disc list-inside space-y-1 text-wrap">
                                    {item.SubjectNames.map((name, idx) => (
                                      <li key={idx}>{name}</li>
                                    ))}
                                  </ul>
                                  <div className="absolute -top-2 left-24 border-8 border-transparent border-b-gray-900"></div>
                                </div>
                              </div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                              {item.LastReminderSentAt ? new Date(item.LastReminderSentAt).toLocaleString() : '-'}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {/* Modals */}
      <ManageRelationshipsModal
        isOpen={showEvaluatorRelationshipsModal}
        onClose={() => {
          setShowEvaluatorRelationshipsModal(false);
          setSelectedEvaluator(null);
        }}
        entityType="evaluator"
        entityId={selectedEvaluator?.id || null}
        entityName={selectedEvaluator?.name || ''}
        projectSlug={projectSlug}
        onRelationshipsUpdated={loadEvaluators}
      />

      <ManageRelationshipsModal
        isOpen={showSubjectRelationshipsModal}
        onClose={() => {
          setShowSubjectRelationshipsModal(false);
          setSelectedSubject(null);
        }}
        entityType="subject"
        entityId={selectedSubject?.id || null}
        entityName={selectedSubject?.name || ''}
        projectSlug={projectSlug}
        onRelationshipsUpdated={loadSubjects}
      />

      <ViewRelationshipsModal
        isOpen={showEvaluatorViewModal}
        onClose={() => {
          setShowEvaluatorViewModal(false);
          setSelectedEvaluatorForView(null);
        }}
        entityType="evaluator"
        entityId={selectedEvaluatorForView?.id || null}
        entityName={selectedEvaluatorForView?.name || ''}
        projectSlug={projectSlug}
      />

      <ViewRelationshipsModal
        isOpen={showSubjectViewModal}
        onClose={() => {
          setShowSubjectViewModal(false);
          setSelectedSubjectForView(null);
        }}
        entityType="subject"
        entityId={selectedSubjectForView?.id || null}
        entityName={selectedSubjectForView?.name || ''}
        projectSlug={projectSlug}
      />
    </div>
  );
}



