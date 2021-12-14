#!/bin/sh

NODE_EXE=node.exe
NPM="cmd.exe /C npm.cmd"
MSBUILD=/mnt/c/Program*Files*/Microsoft*Visual*Studio/2022/*/MSBuild/*/Bin/MSBuild.exe

${NPM} install --target-arch=ia32 --arch=ia32 --scripts-prepend-node-path=true --no-bin-links --cwd Client --prefix Client

${MSBUILD} /restore GrpcJsElectronSharp.sln 

cd Client && ${NPM} run build && cd ..

${NODE_EXE} Client/node_modules/electron-builder/out/cli/cli.js --project='Client' --ia32   
