import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; subjectId: string; evaluatorId: string }> }
) {
  try {
    const { tenantSlug, subjectId, evaluatorId } = await params;
    const authHeader = request.headers.get('authorization');

    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const body = await request.json();
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/subjects/${subjectId}/evaluators/${evaluatorId}`;

    const response = await fetch(backendUrl, {
      method: 'PUT',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    const text = await response.text();

    if (!response.ok) {
      let data;
      try {
        data = text ? JSON.parse(text) : { error: 'Request failed' };
      } catch {
        data = { error: text || 'Request failed' };
      }
      return NextResponse.json(data, { status: response.status });
    }

    // Success with content
    if (text) {
      return NextResponse.json(JSON.parse(text));
    }

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    console.error('Error updating relationship:', error);
    const errorMessage = error instanceof Error ? error.message : 'Failed to update relationship';
    return NextResponse.json(
      { error: errorMessage },
      { status: 500 }
    );
  }
}

export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; subjectId: string; evaluatorId: string }> }
) {
  try {
    const { tenantSlug, subjectId, evaluatorId } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/subjects/${subjectId}/evaluators/${evaluatorId}`;

    const response = await fetch(backendUrl, {
      method: 'DELETE',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
    });

    // Backend returns 204 No Content on success
    if (response.status === 204) {
      return new NextResponse(null, { status: 204 });
    }

    // Only try to parse JSON if there's content
    const text = await response.text();

    if (!response.ok) {
      let data;
      try {
        data = text ? JSON.parse(text) : { error: 'Request failed' };
      } catch {
        data = { error: text || 'Request failed' };
      }
      return NextResponse.json(data, { status: response.status });
    }

    // Success with content
    if (text) {
      return NextResponse.json(JSON.parse(text));
    }

    return new NextResponse(null, { status: 204 });
  } catch (error: unknown) {
    console.error('Error removing relationship:', error);
    const errorMessage = error instanceof Error ? error.message : 'Failed to remove relationship';
    return NextResponse.json(
      { error: errorMessage },
      { status: 500 }
    );
  }
}

