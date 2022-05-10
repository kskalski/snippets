Reproduction of problem with configuring esbuild using vite config (see https://github.com/vitejs/vite/issues/6065)

npm install
npm run build
grep foo dist/assets/*

# Expected output should contain
.foo{color:red}.bar{color:red}

# Actual output contains
.foo,.bar{color:red}

