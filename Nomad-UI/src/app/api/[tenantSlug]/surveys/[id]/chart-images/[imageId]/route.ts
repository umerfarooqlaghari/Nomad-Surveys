import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; id: string; imageId: string }> }
) {
  try {
    const { tenantSlug, id, imageId } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/surveys/${id}/chart-images/${imageId}`;

    const response = await fetch(backendUrl, {
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

