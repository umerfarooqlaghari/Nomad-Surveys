import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string }> }
) {
  try {
    const { tenantSlug } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/report-template-settings/default`;

    const response = await fetch(backendUrl, {
      method: 'GET',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      // If no default template exists, return null (not an error)
      if (response.status === 404) {
        return NextResponse.json(null, { status: 200 });
      }
      
      let errorText = 'Failed to load template settings';
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
    return NextResponse.json(data, { status: 200 });
  } catch (error) {
    console.error('Get default template API Error:', error);
    const errorMessage = error instanceof Error ? error.message : String(error);
    const isConnectionError = errorMessage.includes('ECONNREFUSED') || errorMessage.includes('fetch failed');
    
    if (isConnectionError) {
      console.error(`Backend connection failed. Is the backend running at ${BACKEND_URL}?`);
      return NextResponse.json({ 
        error: 'Backend server connection failed', 
        details: `Cannot connect to backend at ${BACKEND_URL}. Please ensure the backend server is running.`,
        backendUrl: BACKEND_URL
      }, { status: 503 }); // Service Unavailable
    }
    
    return NextResponse.json(
      { error: 'Internal server error', details: errorMessage },
      { status: 500 }
    );
  }
}

