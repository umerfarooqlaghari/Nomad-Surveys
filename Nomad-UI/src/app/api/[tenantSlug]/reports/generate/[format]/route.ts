import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ tenantSlug: string; format: string }> }
) {
  try {
    const { tenantSlug, format } = await params;
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader) {
      return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
    }

    if (!['html', 'pdf'].includes(format)) {
      return NextResponse.json({ error: 'Invalid format. Must be html or pdf' }, { status: 400 });
    }

    const body = await request.json();
    const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/reports/generate/${format}`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      let errorText = 'Failed to generate report';
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

    if (format === 'pdf') {
      const blob = await response.blob();
      return new NextResponse(blob, {
        headers: {
          'Content-Type': 'application/pdf',
          'Content-Disposition': `attachment; filename="report_${Date.now()}.pdf"`,
        },
      });
    } else {
      const html = await response.text();
      return new NextResponse(html, {
        headers: {
          'Content-Type': 'text/html',
        },
      });
    }
  } catch (error) {
    console.error('Generate Report API Error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

