'use client';

import React, { useState } from 'react';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import {
  QuestionMarkCircleIcon,
  EnvelopeIcon,
  ChevronDownIcon,
  ChevronUpIcon,
} from '@heroicons/react/24/outline';

interface FAQ {
  question: string;
  answer: string;
}

export default function HelpSupport() {
  const [expandedFAQ, setExpandedFAQ] = useState<number | null>(null);
  const [contactForm, setContactForm] = useState({
    subject: '',
    message: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitSuccess, setSubmitSuccess] = useState(false);

  const faqs: FAQ[] = [
    {
      question: 'How do I complete an evaluation?',
      answer: 'Navigate to "Assigned Evaluations" from the sidebar, find the evaluation you want to complete, and click the "Start" button. Fill out all required fields and click "Complete" to submit your evaluation.',
    },
    {
      question: 'Can I save my progress and continue later?',
      answer: 'Yes! Your progress is automatically saved as you fill out the evaluation form. You can close the form and return to it later - your answers will be preserved.',
    },
    {
      question: 'What happens after I submit an evaluation?',
      answer: 'Once you submit an evaluation, it will be marked as completed and moved to your "My Submissions" section. You can view a read-only summary of your submission at any time.',
    },
    {
      question: 'Can I edit an evaluation after submitting it?',
      answer: 'No, once an evaluation is submitted, it cannot be edited. Please review your answers carefully before submitting.',
    },
    {
      question: 'How do I know which evaluations are pending?',
      answer: 'Your dashboard shows a summary of pending evaluations. You can also view all pending evaluations in the "Assigned Evaluations" section, where they are clearly marked with a "Pending" status.',
    },
    {
      question: 'What if I have technical issues while filling out an evaluation?',
      answer: 'If you encounter any technical issues, please contact support using the form below. Include details about the issue and which evaluation you were working on.',
    },
    {
      question: 'Are my evaluation responses confidential?',
      answer: 'Yes, your evaluation responses are confidential and will only be accessible to authorized administrators and the evaluation coordinators.',
    },
    {
      question: 'How long do I have to complete an evaluation?',
      answer: 'Each evaluation may have a due date, which is displayed on the evaluation card. If no due date is shown, please complete the evaluation as soon as possible.',
    },
  ];

  const toggleFAQ = (index: number) => {
    setExpandedFAQ(expandedFAQ === index ? null : index);
  };

  const handleSubmitContact = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setIsSubmitting(true);
      // TODO: Implement actual API call to send support message
      console.log('Contact form submitted:', contactForm);
      await new Promise((resolve) => setTimeout(resolve, 1000)); // Simulate API call
      setSubmitSuccess(true);
      setContactForm({ subject: '', message: '' });
      setTimeout(() => setSubmitSuccess(false), 5000);
    } catch (error) {
      console.error('Error submitting contact form:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-2xl font-bold text-black">Help & Support</h1>
            <p className="text-sm text-black mt-1">
              Find answers to common questions or contact support
            </p>
          </div>

          {/* FAQ Section */}
          <div className="bg-white rounded-lg border border-gray-200 mb-8">
            <div className="px-6 py-4 border-b border-gray-200">
              <div className="flex items-center">
                <QuestionMarkCircleIcon className="h-6 w-6 text-blue-600 mr-2" />
                <h2 className="text-lg font-semibold text-black">Frequently Asked Questions</h2>
              </div>
            </div>
            <div className="divide-y divide-gray-200">
              {faqs.map((faq, index) => (
                <div key={index} className="px-6 py-4">
                  <button
                    onClick={() => toggleFAQ(index)}
                    className="w-full flex items-center justify-between text-left"
                  >
                    <span className="text-sm font-medium text-black">{faq.question}</span>
                    {expandedFAQ === index ? (
                      <ChevronUpIcon className="h-5 w-5 text-black flex-shrink-0 ml-4" />
                    ) : (
                      <ChevronDownIcon className="h-5 w-5 text-black flex-shrink-0 ml-4" />
                    )}
                  </button>
                  {expandedFAQ === index && (
                    <div className="mt-3 text-sm text-black pl-0">
                      {faq.answer}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* Contact Support Section */}
          <div className="bg-white rounded-lg border border-gray-200">
            <div className="px-6 py-4 border-b border-gray-200">
              <div className="flex items-center">
                <EnvelopeIcon className="h-6 w-6 text-blue-600 mr-2" />
                <h2 className="text-lg font-semibold text-black">Contact Support</h2>
              </div>
              <p className="text-sm text-black mt-1">
                Can&apos;t find what you&apos;re looking for? Send us a message and we&apos;ll get back to you.
              </p>
            </div>
            <div className="px-6 py-6">
              {submitSuccess && (
                <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg">
                  <p className="text-sm font-medium text-green-800">
                    Your message has been sent successfully! We&apos;ll get back to you soon.
                  </p>
                </div>
              )}
              <form onSubmit={handleSubmitContact}>
                <div className="mb-4">
                  <label htmlFor="subject" className="block text-sm font-medium text-black mb-2">
                    Subject
                  </label>
                  <input
                    type="text"
                    id="subject"
                    value={contactForm.subject}
                    onChange={(e) => setContactForm({ ...contactForm, subject: e.target.value })}
                    required
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg text-sm text-black placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="Brief description of your issue"
                  />
                </div>
                <div className="mb-4">
                  <label htmlFor="message" className="block text-sm font-medium text-black mb-2">
                    Message
                  </label>
                  <textarea
                    id="message"
                    value={contactForm.message}
                    onChange={(e) => setContactForm({ ...contactForm, message: e.target.value })}
                    required
                    rows={6}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg text-sm text-black placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                    placeholder="Describe your issue or question in detail..."
                  />
                </div>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isSubmitting ? 'Sending...' : 'Send Message'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}

