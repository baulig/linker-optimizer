# Bootstrapping the Blazor Sample

## Prerequisites

```
$ dotnet --version
3.0.100-preview6-012266
$ dotnet new -i Microsoft.AspNetCore.Blazor.Templates::3.0.0-preview6.19307.2
```

Create it with:

    $ dotnet new blazor -n HelloBlazor

## Build Mono WebAssembly

Make sure to create a `Make.config` because it would otherwise build the entire world.

```
$ cat ../Make.config
DISABLE_ANDROID = 1
DISABLE_IOS = 1
DISABLE_MAC = 1
DISABLE_WASM_CROSS = 1
DISABLE_LLVM = 1
DISABLE_DESKTOP = 1

# DISABLE_BCL = 1
```

After everything is built, do a `make` in `sdks/wasm`.  This seems to crash with dotnet preview7 (lastest master dogfood), but works fine with preview6.

### Building

Checkout the Blazor module:

```
$ git remote -v
$ git remote -v
baulig	git@github.com:baulig/Blazor.git (fetch)
baulig	git@github.com:baulig/Blazor.git (push)
origin	https://github.com/aspnet/Blazor.git (fetch)
origin	https://github.com/aspnet/Blazor.git (push)
```

In this module, you will find a file called [`src/Microsoft.AspNetCore.Blazor.Mono/HowToUpgradeMono.md`](
https://github.com/aspnet/Blazor/blob/master/src/Microsoft.AspNetCore.Blazor.Mono/HowToUpgradeMono.md).

