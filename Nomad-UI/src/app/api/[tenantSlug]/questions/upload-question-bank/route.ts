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
        const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/questions/upload-question-bank`;

        const response = await fetch(backendUrl, {
            method: 'POST',
            headers: {
                'Authorization': authHeader,
            },
            body: formData,
        });

        const data = await response.json();

        if (!response.ok) {
            return NextResponse.json(data, { status: response.status });
        }

        return NextResponse.json(data);
    } catch (error) {
        console.error('Error uploading question bank:', error);
        return NextResponse.json(
            { error: 'Failed to upload question bank', details: error instanceof Error ? error.message : String(error) },
            { status: 500 }
        );
    }
}
