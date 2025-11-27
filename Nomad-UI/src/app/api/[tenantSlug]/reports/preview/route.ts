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

    const body = await request.json();
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/reports/preview`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    // Handle response
    const contentType = response.headers.get('content-type');
    
    if (!response.ok) {
      let errorText = 'Failed to generate preview';
      try {
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

    // Read HTML response
    const html = await response.text();
    return new NextResponse(html, {
      headers: {
        'Content-Type': 'text/html',
      },
    });
  } catch (error) {
    console.error('Preview API Error:', error);
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
    
    return NextResponse.json({ 
      error: 'Internal server error', 
      details: errorMessage 
    }, { status: 500 });
  }
}

