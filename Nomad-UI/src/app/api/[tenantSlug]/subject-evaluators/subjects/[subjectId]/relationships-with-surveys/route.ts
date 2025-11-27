import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; subjectId: string }> }
) {
  try {
    const { tenantSlug, subjectId } = await params;
    const authHeader = request.headers.get('authorization');

    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/subjects/${subjectId}/relationships-with-surveys`;

    const response = await fetch(backendUrl, {
      method: 'GET',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json'
      }
    });

    // Handle empty response body
    const text = await response.text();
    let data;

    try {
      data = text ? JSON.parse(text) : null;
    } catch (error) {
      console.error('Failed to parse response:', text);
      return NextResponse.json(
        { error: 'Invalid response from backend', details: text },
        { status: 500 }
      );
    }

    if (!response.ok) {
      return NextResponse.json(data || { error: 'Request failed' }, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Error fetching subject relationships with surveys:', error);
    return NextResponse.json(
      { error: 'Failed to fetch subject relationships with surveys' },
      { status: 500 }
    );
  }
}

