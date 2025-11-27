import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string }> }
) {
  try {
    const body = await request.json();
    const { tenantSlug } = await params;
    const authHeader = request.headers.get('Authorization');
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/Email/send-form-reminder`;

    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (authHeader) {
      headers['Authorization'] = authHeader;
    }

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify(body),
    });

    const data = await response.json();
    
    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Email API Error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

