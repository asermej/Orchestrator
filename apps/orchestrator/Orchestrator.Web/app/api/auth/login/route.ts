import { auth0 } from '@/lib/auth0';
import { NextRequest } from 'next/server';

export async function GET(request: NextRequest) {
  const returnTo = request.nextUrl.searchParams.get('returnTo') || '/';

  return await auth0.startInteractiveLogin({
    returnTo,
  });
}

