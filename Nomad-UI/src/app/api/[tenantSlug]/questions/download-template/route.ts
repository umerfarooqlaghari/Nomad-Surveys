import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5232';

export async function GET(
    request: NextRequest,
    { params }: { params: Promise<{ tenantSlug: string }> }
) {
    try {
        const { tenantSlug } = await params;
        const authHeader = request.headers.get('authorization');

        if (!authHeader) {
            return NextResponse.json({ error: 'Authorization header required' }, { status: 401 });
        }

        const backendUrl = `${BACKEND_URL}/${tenantSlug}/api/questions/download-template`;

        const response = await fetch(backendUrl, {
            method: 'GET',
            headers: {
                'Authorization': authHeader,
            },
        });

        if (!response.ok) {
            const data = await response.json();
            return NextResponse.json(data, { status: response.status });
        }

        const blob = await response.blob();
        return new NextResponse(blob, {
            status: 200,
            headers: {
                'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                'Content-Disposition': `attachment; filename="QuestionBank_Template.xlsx"`,
            },
        });
    } catch (error) {
        console.error('Error downloading template:', error);
        return NextResponse.json(
            { error: 'Failed to download template' },
            { status: 500 }
        );
    }
}
