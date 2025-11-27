import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string }> }
) {
  try {
    const { tenantSlug } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    // Forward the multipart form data to the backend
    const formData = await request.formData();
    
    // Log form data for debugging
    console.log('Received form data with fields:', Array.from(formData.keys()));
    const fileEntries = Array.from(formData.entries()).filter(([key, value]) => value instanceof File);
    console.log('Files in form data:', fileEntries.map(([key, file]) => ({ key, name: (file as File).name, size: (file as File).size })));
    
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/report-template-settings/save`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': authHeader,
        // Don't set Content-Type header - let fetch set it with boundary for multipart/form-data
      },
      body: formData,
    });

    if (!response.ok) {
      let errorText = 'Failed to save template settings';
      try {
        const contentType = response.headers.get('content-type');
        if (contentType?.includes('application/json')) {
          const errorJson = await response.json();
          errorText = errorJson.message || errorJson.error || errorText;
        } else {
          errorText = await response.text();
        }
      } catch {
        errorText = `HTTP ${response.status}: ${response.statusText}`;
      }
      console.error('Backend error:', errorText);
      return NextResponse.json({ error: errorText }, { status: response.status });
    }

    const data = await response.json();
    return NextResponse.json(data, { status: response.status });
  } catch (error) {
    console.error('Save template API Error:', error);
    return NextResponse.json(
      { error: 'Internal server error', details: error instanceof Error ? error.message : String(error) },
      { status: 500 }
    );
  }
}

