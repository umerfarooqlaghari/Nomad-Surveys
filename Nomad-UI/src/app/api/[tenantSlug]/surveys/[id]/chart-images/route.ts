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

    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/surveys/${id}/chart-images`;

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
    console.error('Error fetching chart images:', error);
    return NextResponse.json(
      { error: 'Failed to fetch chart images' },
      { status: 500 }
    );
  }
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; id: string }> }
) {
  try {
    const { tenantSlug, id } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const body = await request.json();
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/surveys/${id}/chart-images`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    const data = await response.json();

    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    // Return 200 for success (even if backend returns 201) so frontend response.ok works
    return NextResponse.json(data, { status: 200 });
  } catch (error) {
    console.error('Error creating chart image:', error);
    return NextResponse.json(
      { error: 'Failed to create chart image' },
      { status: 500 }
    );
  }
}

export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; id: string }> }
) {
  try {
    const { tenantSlug, id } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const { searchParams } = new URL(request.url);
    const imageType = searchParams.get('imageType');
    const clusterName = searchParams.get('clusterName');
    const competencyName = searchParams.get('competencyName');

    const backendUrl = new URL(`${BACKEND_URL}/${tenantSlug}/api/surveys/${id}/chart-images`);
    if (imageType) backendUrl.searchParams.set('imageType', imageType);
    if (clusterName) backendUrl.searchParams.set('clusterName', clusterName);
    if (competencyName) backendUrl.searchParams.set('competencyName', competencyName);

    const response = await fetch(backendUrl.toString(), {
      method: 'DELETE',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
    });

    if (response.status === 204) {
      return new NextResponse(null, { status: 204 });
    }

    const data = await response.json();
    
    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Error deleting chart image:', error);
    return NextResponse.json(
      { error: 'Failed to delete chart image' },
      { status: 500 }
    );
  }
}

