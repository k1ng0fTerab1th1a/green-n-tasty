import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dirname = typeof __dirname !== 'undefined' ? __dirname : path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');

    return {
        plugins: [react()],
        server: {
            proxy: {
                '/dishes': {
                    target: env.VITE_API_URL,
                    changeOrigin: true,
                    secure: false,
                },
            },
        },
        test: {
            globals: true,
            environment: 'jsdom',
        }
    };
});