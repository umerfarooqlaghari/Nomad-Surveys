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

    // Forward query parameters (e.g., ?clusterId=xxx)
    const searchParams = request.nextUrl.searchParams;
    const queryString = searchParams.toString();
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/competencies${queryString ? `?${queryString}` : ''}`;

    const response = await fetch(backendUrl, {
      method: 'GET',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
    });

    const data = await response.json();
    
    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Error fetching competencies:', error);
    return NextResponse.json(
      { error: 'Failed to fetch competencies' },
      { status: 500 }
    );
  }
}

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
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/competencies`;

    console.log('[Competencies POST] Calling backend:', backendUrl);

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    console.log('[Competencies POST] Backend response status:', response.status);

    // Check if response has content
    const contentType = response.headers.get('content-type');
    let data;

    if (contentType && contentType.includes('application/json')) {
      data = await response.json();
    } else {
      const text = await response.text();
      console.error('[Competencies POST] Non-JSON response:', text);
      data = { error: 'Invalid response from backend' };
    }

    if (!response.ok) {
      console.error('[Competencies POST] Backend error:', data);
      return NextResponse.json(data, { status: response.status });
    }

    console.log('[Competencies POST] Success:', data);
    return NextResponse.json(data, { status: response.status });
  } catch (error) {
    console.error('[Competencies POST] Exception:', error);
    return NextResponse.json(
      { error: 'Failed to create competency', details: error instanceof Error ? error.message : String(error) },
      { status: 500 }
    );
  }
}

