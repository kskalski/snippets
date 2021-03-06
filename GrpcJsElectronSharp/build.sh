#!/bin/sh

NODE_EXE=/mnt/d/rep/Kogut/packages/nodejsandnpm/10.0.3/node.exe
NPM="${NODE_EXE} /rep/Kogut/packages/nodejsandnpm/10.0.3/node_modules/npm/bin/npm-cli.js"
MSBUILD=/mnt/c/Program*Files*x86*/Microsoft*Visual*Studio/2019/*/MSBuild/*/Bin/MSBuild.exe

${NPM} install --target-arch=ia32 --arch=ia32 --scripts-prepend-node-path=true --no-bin-links --cwd Client --prefix Client

${MSBUILD} /restore GrpcJsElectronSharp.sln 

${NODE_EXE} Client/node_modules/electron-builder/out/cli/cli.js --project='Client' --ia32   
