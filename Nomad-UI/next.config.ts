import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */

  // Step 4: Disable React StrictMode to prevent SurveyJS double-mount issues in development
  // React StrictMode intentionally mounts components twice in dev mode for debugging
  // SurveyJS doesn't handle this properly and resets internal DOM state between mounts
  reactStrictMode: false,
};

export default nextConfig;
