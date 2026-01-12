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
        const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/reports/generate/excel/ratee-average`;

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

        const blob = await response.blob();

        // Get the filename from the backend response (Content-Disposition header)
        const contentDisposition = response.headers.get('content-disposition');
        let filename = `Ratee_Average_Report_${Date.now()}.xlsx`;

        if (contentDisposition) {
            // Parse filename from Content-Disposition header
            const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
            if (filenameMatch && filenameMatch[1]) {
                filename = filenameMatch[1].replace(/['"]/g, '');
            }
        }

        return new NextResponse(blob, {
            headers: {
                'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                'Content-Disposition': `attachment; filename="${filename}"`,
            },
        });

    } catch (error) {
        console.error('Generate Ratee Average Report API Error:', error);
        return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
    }
}
