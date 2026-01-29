import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function GET(
    request: NextRequest,
    { params }: { params: Promise<{ tenantSlug: string }> }
) {
    try {
        const { tenantSlug } = await params;
        const authHeader = request.headers.get('Authorization');
        // For SubjectEvaluatorController, the route is {tenantSlug}/api/subject-evaluators/emailing-list
        const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/subject-evaluators/emailing-list`;

        const headers: HeadersInit = {
            'Content-Type': 'application/json',
        };

        if (authHeader) {
            headers['Authorization'] = authHeader;
        }

        const response = await fetch(backendUrl, {
            method: 'GET',
            headers,
        });

        if (!response.ok) {
            try {
                const errorData = await response.json();
                return NextResponse.json(errorData, { status: response.status });
            } catch {
                return NextResponse.json({ error: response.statusText }, { status: response.status });
            }
        }

        const data = await response.json();
        return NextResponse.json(data);
    } catch (error) {
        console.error('Emailing List API Error:', error);
        return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
    }
}
