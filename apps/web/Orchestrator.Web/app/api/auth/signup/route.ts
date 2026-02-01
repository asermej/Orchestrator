import { auth0 } from '@/lib/auth0';

export async function GET() {
  return await auth0.startInteractiveLogin({
    authorizationParameters: {
      screen_hint: 'signup',
    },
  });
}

