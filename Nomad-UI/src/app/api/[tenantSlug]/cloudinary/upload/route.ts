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

    const formData = await request.formData();
    const folder = request.nextUrl.searchParams.get('folder') || undefined;
    
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/cloudinary/upload${folder ? `?folder=${encodeURIComponent(folder)}` : ''}`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': authHeader,
      },
      body: formData,
    });

    if (!response.ok) {
      let errorText = 'Failed to upload image';
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
      return NextResponse.json({ error: errorText }, { status: response.status });
    }

    const data = await response.json();
    return NextResponse.json(data, { status: 200 });
  } catch (error) {
    console.error('Upload image API Error:', error);
    return NextResponse.json(
      { error: 'Internal server error', details: error instanceof Error ? error.message : String(error) },
      { status: 500 }
    );
  }
}





