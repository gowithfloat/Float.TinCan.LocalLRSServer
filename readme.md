## Float.TinCan.LocalLRSServer [![Test](https://github.com/gowithfloat/Float.TinCan.LocalLRSServer/actions/workflows/test.yml/badge.svg)](https://github.com/gowithfloat/Float.TinCan.LocalLRSServer/actions/workflows/test.yml) [![NuGet](https://img.shields.io/nuget/v/Float.TinCan.LocalLRSServer)](https://www.nuget.org/packages/Float.TinCan.LocalLRSServer/)

A local LRS server for xAPI.

## Building

First you must restore the nuget libraries

     dotnet restore

This project can be built using [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) or [Cake](https://cakebuild.net/). It is recommended that you build this project by invoking the bootstrap script:

    ./build.sh

There are a number of optional arguments that can be provided to the bootstrapper that will be parsed and passed on to Cake itself. See the [Cake build file](./build.cake) in order to identify all supported parameters.

    ./build.sh \
        --task=Build \
        --projectName=Float.TinCan.LocalLRSServer \
        --configuration=Debug \
        --nugetUrl=https://nuget.org \
        --nugetToken=####

## License

All content in this repository is shared under an MIT license. See [license.md](./license.md) for details.
