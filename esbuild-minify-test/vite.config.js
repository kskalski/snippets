import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  build: {
    minifySyntax: false
  },
  esbuild: {
    minify: false,
    minifySyntax: false
  },
  plugins: [vue()]
})
