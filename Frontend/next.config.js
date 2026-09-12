/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone', // Rất quan trọng để build Docker tối ưu
  eslint: {
    ignoreDuringBuilds: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  }
};

module.exports = nextConfig;
