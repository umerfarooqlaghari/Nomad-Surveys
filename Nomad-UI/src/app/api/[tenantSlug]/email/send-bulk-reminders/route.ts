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
        // For EmailController, the route is {tenantSlug}/api/Email/send-bulk-reminders
        const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/Email/send-bulk-reminders`;

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
        console.error('Bulk Reminders API Error:', error);
        return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
    }
}
