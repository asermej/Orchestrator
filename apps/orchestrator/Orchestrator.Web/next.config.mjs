/** @type {import('next').NextConfig} */
const nextConfig = {
  eslint: {
    ignoreDuringBuilds: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  },
  // Reduces "preloaded using link preload but not used" console warnings (Next.js NEXT-1307).
  // Loads CSS in import order; can reduce preload timing issues.
  experimental: {
    cssChunking: "strict",
  },
  images: {
    unoptimized: true,
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5000',
        pathname: '/uploads/**',
      },
    ],
  },
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000',
  },
  async rewrites() {
    return [
      {
        source: '/uploads/:path*',
        destination: 'http://localhost:5000/uploads/:path*',
      },
    ];
  },
  // Reduce logging noise in development
  logging: {
    fetches: {
      fullUrl: false, // Don't log full URLs for fetches
    },
  },
}

export default nextConfig