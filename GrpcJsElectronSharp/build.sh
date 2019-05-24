#!/bin/sh

NODE_EXE=/mnt/d/rep/Kogut/packages/nodejsandnpm/10.0.3/node.exe
NPM="${NODE_EXE} /rep/Kogut/packages/nodejsandnpm/10.0.3/node_modules/npm/bin/npm-cli.js"
MSBUILD=/mnt/c/Program*Files*x86*/Microsoft*Visual*Studio/2017/*/MSBuild/*/Bin/MSBuild.exe

${NPM} install --target-arch=ia32 --arch=ia32 --scripts-prepend-node-path=true --cwd Client --prefix Client
${NODE_EXE} Client/node_modules/protobufjs/bin/pbjs -t static-module -w commonjs -o Client/protos.js protos/*.proto
${NODE_EXE} Client/node_modules/protobufjs/bin/pbts -o Client/protos.d.ts Client/protos.js

${MSBUILD} /restore GrpcJsElectronSharp.sln 

${NODE_EXE} Client/node_modules/electron-builder/out/cli/cli.js --project='Client' --ia32   
