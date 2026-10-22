import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'dk.emilvadgaardnielsen.handleliste',
  appName: 'HandleListen',
  webDir: 'dist/HandleListen-web/browser',
  // Loads the live production site instead of a bundled copy, so deploying the website as
  // usual updates the app immediately - no new APK needed while we're still iterating.
  server: {
    url: 'https://emilvadgaardnielsen.dk',
    cleartext: false
  }
};

export default config;
