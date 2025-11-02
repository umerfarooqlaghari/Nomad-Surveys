import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; id: string }> }
) {
  try {
    const { tenantSlug, id } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    // Get query parameters for search and filter
    const searchParams = request.nextUrl.searchParams;
    const search = searchParams.get('search');
    const relationshipType = searchParams.get('relationshipType');

    let backendUrl = `${BACKEND_URL}/${tenantSlug}/api/surveys/${id}/available-relationships`;
    
    // Add query parameters if they exist
    const queryParams = new URLSearchParams();
    if (search) queryParams.append('search', search);
    if (relationshipType) queryParams.append('relationshipType', relationshipType);
    
    if (queryParams.toString()) {
      backendUrl += `?${queryParams.toString()}`;
    }

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
    console.error('Error fetching available relationships:', error);
    return NextResponse.json(
      { error: 'Failed to fetch available relationships' },
      { status: 500 }
    );
  }
}

