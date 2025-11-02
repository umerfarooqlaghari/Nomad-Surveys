'use client';

import React, { useState, useEffect, useMemo } from 'react';
import styles from './RelationshipModal.module.css';

// Export types for use in parent components
export interface SelectedRelationship {
  employeeId: string;
  relationship: string;
  isExisting?: boolean;
  isModified?: boolean;
  shouldRemove?: boolean;
  originalRelationship?: string;
}

export interface ExistingRelationship {
  Id: string;
  EmployeeId: string;
  Relationship: string;
}

export interface Employee {
  Id: string;
  EmployeeId: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
}

interface RelationshipModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (relationships: SelectedRelationship[]) => void;
  employees: Employee[];
  existingRelationships: ExistingRelationship[];
  title: string;
}

const RELATIONSHIP_TYPES = [
  { value: 'self', label: 'Self' },
  { value: 'manager', label: 'Manager' },
  { value: 'peer', label: 'Peer' },
  { value: 'subordinate', label: 'Subordinate' },
  { value: 'directreport', label: 'Direct Report' },
  { value: 'colleague', label: 'Colleague' },
  { value: 'lead', label: 'Lead' },
  { value: 'trainee', label: 'Trainee' },
  { value: 'mentor', label: 'Mentor' },
  { value: 'other', label: 'Other' },
];

export default function RelationshipModal({
  isOpen,
  onClose,
  onSave,
  employees,
  existingRelationships,
  title,
}: RelationshipModalProps) {
  const [activeTab, setActiveTab] = useState<'new' | 'existing'>('new');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRelationships, setSelectedRelationships] = useState<Map<string, SelectedRelationship>>(new Map());
  const [existingSearchQuery, setExistingSearchQuery] = useState('');

  // Reset state when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setActiveTab('new');
      setSearchQuery('');
      setExistingSearchQuery('');

      // Initialize existing relationships
      const existingMap = new Map<string, SelectedRelationship>();
      existingRelationships.forEach(rel => {
        existingMap.set(rel.Id, {
          employeeId: rel.Id,
          relationship: rel.Relationship || 'peer',
          isExisting: true,
          isModified: false,
          shouldRemove: false,
          originalRelationship: rel.Relationship || 'peer',
        });
      });
      setSelectedRelationships(existingMap);
    }
  }, [isOpen, existingRelationships]);

  // Filter employees for "Add New" tab (exclude existing relationships)
  const availableEmployees = useMemo(() => {
    const existingIds = new Set(existingRelationships.map(r => r.Id));
    return employees.filter(emp => !existingIds.has(emp.Id));
  }, [employees, existingRelationships]);

  // Filter employees based on search query
  const filteredEmployees = useMemo(() => {
    if (!searchQuery.trim()) return availableEmployees;

    const query = searchQuery.toLowerCase();
    return availableEmployees.filter(emp =>
      emp.FullName.toLowerCase().includes(query) ||
      emp.FirstName.toLowerCase().includes(query) ||
      emp.LastName.toLowerCase().includes(query) ||
      emp.EmployeeId.toLowerCase().includes(query) ||
      emp.Email.toLowerCase().includes(query)
    );
  }, [availableEmployees, searchQuery]);

  // Filter existing relationships based on search query
  const filteredExistingRelationships = useMemo(() => {
    if (!existingSearchQuery.trim()) return existingRelationships;

    const query = existingSearchQuery.toLowerCase();
    return existingRelationships.filter(rel => {
      const employee = employees.find(e => e.Id === rel.Id);
      if (!employee) return false;

      return (
        employee.FullName.toLowerCase().includes(query) ||
        employee.FirstName.toLowerCase().includes(query) ||
        employee.LastName.toLowerCase().includes(query) ||
        employee.EmployeeId.toLowerCase().includes(query) ||
        employee.Email.toLowerCase().includes(query) ||
        rel.Relationship.toLowerCase().includes(query)
      );
    });
  }, [existingRelationships, existingSearchQuery, employees]);

  // Toggle employee selection
  const toggleEmployee = (employeeId: string) => {
    console.log('👆 Toggle employee CALLED', { id: employeeId, currentlySelected: selectedRelationships.has(employeeId) });

    setSelectedRelationships(prev => {
      const newMap = new Map(prev);

      if (newMap.has(employeeId)) {
        // If it's an existing relationship, don't remove it from the map
        const existing = newMap.get(employeeId);
        if (existing?.isExisting) {
          return newMap;
        }
        // Remove new selection
        newMap.delete(employeeId);
        console.log('➖ Employee DESELECTED', { id: employeeId, newSize: newMap.size });
      } else {
        // Add new selection with default relationship
        newMap.set(employeeId, {
          employeeId,
          relationship: 'peer',
          isExisting: false,
          isModified: false,
          shouldRemove: false,
        });
        console.log('➕ Employee SELECTED', { id: employeeId, newSize: newMap.size });
      }

      console.log('📊 New selectedRelationships Map:', Array.from(newMap.entries()));
      return newMap;
    });
  };

  // Update relationship type
  const updateRelationship = (employeeId: string, relationship: string) => {
    console.log('🔄 Relationship changed', { employeeId, relationship });

    setSelectedRelationships(prev => {
      const newMap = new Map(prev);
      const existing = newMap.get(employeeId);

      if (existing) {
        newMap.set(employeeId, {
          ...existing,
          relationship,
          isModified: existing.isExisting && existing.originalRelationship !== relationship,
        });
      }

      return newMap;
    });
  };

  // Toggle remove for existing relationship
  const toggleRemove = (employeeId: string) => {
    setSelectedRelationships(prev => {
      const newMap = new Map(prev);
      const existing = newMap.get(employeeId);

      if (existing && existing.isExisting) {
        newMap.set(employeeId, {
          ...existing,
          shouldRemove: !existing.shouldRemove,
        });
      }

      return newMap;
    });
  };

  // Calculate counts for save button
  const getCounts = () => {
    const relationships = Array.from(selectedRelationships.values());
    const newCount = relationships.filter(r => !r.isExisting && !r.shouldRemove).length;
    const modifiedCount = relationships.filter(r => r.isExisting && r.isModified && !r.shouldRemove).length;
    const removedCount = relationships.filter(r => r.shouldRemove).length;
    const total = newCount + modifiedCount + removedCount;

    return { newCount, modifiedCount, removedCount, total };
  };

  // Check if save button should be disabled
  const isSaveDisabled = () => {
    const counts = getCounts();
    if (counts.total === 0) return true;

    // Check if any new selection doesn't have a relationship type
    const relationships = Array.from(selectedRelationships.values());
    const hasInvalidSelection = relationships.some(r =>
      !r.isExisting && !r.shouldRemove && !r.relationship
    );

    return hasInvalidSelection;
  };

  // Handle save
  const handleSave = () => {
    console.log('💾 Save initiated');

    const relationships = Array.from(selectedRelationships.values());
    const counts = getCounts();

    console.log('📦 Save payload', {
      counts,
      payload: relationships,
    });

    console.log('➡️ Calling onSave...');
    onSave(relationships);
    console.log('✅ onSave completed successfully');

    onClose();
  };

  // Handle close
  const handleClose = () => {
    setSearchQuery('');
    setExistingSearchQuery('');
    setSelectedRelationships(new Map());
    onClose();
  };

  if (!isOpen) return null;

  const counts = getCounts();

  return (
    <div className={styles.overlay} onClick={handleClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className={styles.header}>
          <h2 className={styles.title}>{title}</h2>
          <button
            className={styles.closeButton}
            onClick={handleClose}
            aria-label="Close modal"
          >
            ×
          </button>
        </div>

        {/* Tabs */}
        <div className={styles.tabs}>
          <button
            className={`${styles.tab} ${activeTab === 'new' ? styles.activeTab : ''}`}
            onClick={() => setActiveTab('new')}
          >
            Add New Relationship
            {counts.newCount > 0 && (
              <span className={styles.tabBadge}>{counts.newCount}</span>
            )}
          </button>
          <button
            className={`${styles.tab} ${activeTab === 'existing' ? styles.activeTab : ''}`}
            onClick={() => setActiveTab('existing')}
          >
            Existing Relationships
            <span className={styles.tabBadge}>{existingRelationships.length}</span>
          </button>
        </div>

        {/* Content */}
        <div className={styles.content}>
          {activeTab === 'new' ? (
            <>
              {/* Search */}
              <div className={styles.searchSection}>
                <input
                  type="text"
                  className={styles.searchInput}
                  placeholder="Search by name, employee ID, or email..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>

              {/* Employee List */}
              <div className={styles.employeeList}>
                {filteredEmployees.length === 0 ? (
                  <div className={styles.emptyState}>
                    <p className={styles.emptyStateText}>
                      {searchQuery
                        ? 'No employees found matching your search.'
                        : 'No available employees to add.'}
                    </p>
                  </div>
                ) : (
                  <div className={styles.tableContainer}>
                    <div className={styles.tableHeader}>
                      <div>Person</div>
                      <div>Relationship Type</div>
                    </div>
                    <div className={styles.tableBody}>
                      {filteredEmployees.map((employee) => {
                        const isSelected = selectedRelationships.has(employee.Id);
                        const relationship = selectedRelationships.get(employee.Id);

                        return (
                          <div
                            key={employee.Id}
                            className={`${styles.tableRow} ${isSelected ? styles.selected : ''}`}
                          >
                            <div
                              className={styles.columnPerson}
                              onClick={() => toggleEmployee(employee.Id)}
                            >
                              <input
                                type="checkbox"
                                className={styles.checkbox}
                                checked={isSelected}
                                onChange={() => toggleEmployee(employee.Id)}
                                onClick={(e) => e.stopPropagation()}
                              />
                              <div className={styles.employeeDetails}>
                                <p className={styles.employeeName}>{employee.FullName}</p>
                                <p className={styles.employeeMeta}>
                                  {employee.EmployeeId} • {employee.Email}
                                </p>
                              </div>
                            </div>
                            <div className={styles.columnRelationship}>
                              {isSelected ? (
                                <select
                                  className={styles.relationshipSelect}
                                  value={relationship?.relationship || 'peer'}
                                  onChange={(e) => updateRelationship(employee.Id, e.target.value)}
                                  onClick={(e) => e.stopPropagation()}
                                >
                                  {RELATIONSHIP_TYPES.map((type) => (
                                    <option key={type.value} value={type.value}>
                                      {type.label}
                                    </option>
                                  ))}
                                </select>
                              ) : (
                                <span className={styles.placeholderText}>Select person first</span>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
              </div>
            </>
          ) : (
            <>
              {/* Existing Relationships Tab */}
              <div className={styles.searchSection}>
                <input
                  type="text"
                  className={styles.searchInput}
                  placeholder="Search existing relationships..."
                  value={existingSearchQuery}
                  onChange={(e) => setExistingSearchQuery(e.target.value)}
                />
              </div>

              <div className={styles.employeeList}>
                {filteredExistingRelationships.length === 0 ? (
                  <div className={styles.emptyState}>
                    <p className={styles.emptyStateText}>
                      {existingSearchQuery
                        ? 'No relationships found matching your search.'
                        : 'No existing relationships.'}
                    </p>
                  </div>
                ) : (
                  <div className={styles.tableContainer}>
                    <div className={`${styles.tableHeader} ${styles.tableHeaderWithActions}`}>
                      <div>Person</div>
                      <div>Relationship Type</div>
                      <div>Actions</div>
                    </div>
                    <div className={styles.tableBody}>
                      {filteredExistingRelationships.map((rel) => {
                        const employee = employees.find(e => e.Id === rel.Id);
                        if (!employee) return null;

                        const relationship = selectedRelationships.get(rel.Id);
                        const shouldRemove = relationship?.shouldRemove || false;
                        const isModified = relationship?.isModified || false;

                        return (
                          <div
                            key={rel.Id}
                            className={`${styles.tableRow} ${styles.tableRowWithActions} ${shouldRemove ? styles.markedForRemoval : ''}`}
                          >
                            <div className={styles.columnPerson}>
                              <div className={styles.employeeDetails}>
                                <p className={styles.employeeName}>{employee.FullName}</p>
                                <p className={styles.employeeMeta}>
                                  {employee.EmployeeId} • {employee.Email}
                                </p>
                              </div>
                            </div>
                            <div className={styles.columnRelationship}>
                              <select
                                className={styles.relationshipSelect}
                                value={relationship?.relationship || rel.Relationship}
                                onChange={(e) => updateRelationship(rel.Id, e.target.value)}
                                disabled={shouldRemove}
                              >
                                {RELATIONSHIP_TYPES.map((type) => (
                                  <option key={type.value} value={type.value}>
                                    {type.label}
                                  </option>
                                ))}
                              </select>
                              {isModified && !shouldRemove && (
                                <span className={styles.modifiedBadge}>Modified</span>
                              )}
                            </div>
                            <div className={styles.columnActions}>
                              <button
                                className={shouldRemove ? styles.undoButton : styles.removeButton}
                                onClick={() => toggleRemove(rel.Id)}
                              >
                                {shouldRemove ? 'Undo' : 'Remove'}
                              </button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className={styles.footer}>
          <button
            className={styles.cancelButton}
            onClick={handleClose}
          >
            Cancel
          </button>
          <button
            className={styles.saveButton}
            onClick={handleSave}
            disabled={isSaveDisabled()}
          >
            Save Changes
            {counts.total > 0 && (
              <span className={styles.saveBadge}>
                {counts.newCount > 0 && `+${counts.newCount}`}
                {counts.modifiedCount > 0 && ` ~${counts.modifiedCount}`}
                {counts.removedCount > 0 && ` -${counts.removedCount}`}
              </span>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
