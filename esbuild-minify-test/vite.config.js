import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  esbuild: {
    minify: false,
    minifySyntax: false
  },
  plugins: [vue()]
})
