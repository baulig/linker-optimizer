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

#### Blazor

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

Updating:

```
$ cd src/Microsoft.AspNetCore.Blazor.Mono/
$ cp /Workspace/mono-linker/sdks/wasm/debug/mono.* ./incoming/wasm/
$ rm -f incoming/bcl/*.dll incoming/bcl/Facades/*.dll
$ cp -a /Workspace/mono-linker/sdks/out/wasm-bcl/wasm/*.dll ./incoming/bcl/
$ cp -a /Workspace/mono-linker/sdks/out/wasm-bcl/wasm/Facades/*.dll ./incoming/bcl/Facades/
$ rm ./incoming/bcl/nunitlite.dll
```

Then build:

```
$ ./build.sh 
Downloading 'https://dot.net/v1/dotnet-install.sh'
dotnet-install: Downloading link: https://dotnetcli.azureedge.net/dotnet/Sdk/3.0.100-preview6-012264/dotnet-sdk-3.0.100-preview6-012264-osx-x64.tar.gz
dotnet-install: Extracting zip from https://dotnetcli.azureedge.net/dotnet/Sdk/3.0.100-preview6-012264/dotnet-sdk-3.0.100-preview6-012264-osx-x64.tar.gz
dotnet-install: Adding to current process PATH: `/Workspace/Blazor/.dotnet`. Note: This change will be visible only when sourcing script.
dotnet-install: Installation finished successfully.
  Restore completed in 261.6 ms for /Users/mabaul/.nuget/packages/microsoft.dotnet.arcade.sdk/1.0.0-beta.19323.4/tools/Tools.proj.
dotnet-install: Downloading link: https://dotnetcli.azureedge.net/dotnet/Runtime/2.1.11/dotnet-runtime-2.1.11-osx-x64.tar.gz
dotnet-install: Extracting zip from https://dotnetcli.azureedge.net/dotnet/Runtime/2.1.11/dotnet-runtime-2.1.11-osx-x64.tar.gz
dotnet-install: Adding to current process PATH: `/Workspace/Blazor/.dotnet`. Note: This change will be visible only when sourcing script.
dotnet-install: Installation finished successfully.
  Restore completed in 5.35 sec for /Workspace/Blazor/src/Microsoft.AspNetCore.Blazor.Mono/Microsoft.AspNetCore.Blazor.Mono.csproj.
  Restore completed in 5.73 sec for /Workspace/Blazor/src/Microsoft.AspNetCore.Blazor.BuildTools/Microsoft.AspNetCore.Blazor.BuildTools.csproj.
  Microsoft.AspNetCore.Blazor.Mono -> /Workspace/Blazor/artifacts/bin/Microsoft.AspNetCore.Blazor.Mono/Debug/netstandard1.0/Microsoft.AspNetCore.Blazor.Mono.dll
  Creating optimized Mono WebAssembly build
  Microsoft.AspNetCore.Blazor.BuildTools -> /Workspace/Blazor/artifacts/bin/Microsoft.AspNetCore.Blazor.BuildTools/Debug/netcoreapp2.1/Microsoft.AspNetCore.Blazor.BuildTools.dll
  Creating optimized BCL build
  System.Net.Http.dll                      0.163 MB ==> 0.161 MB
  mscorlib.dll                             3.737 MB ==> 3.737 MB
  Successfully created package '/Workspace/Blazor/artifacts/packages/Debug/Shipping/Microsoft.AspNetCore.Blazor.Mono.0.10.0-dev.nupkg'.

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:12.88
```

This will create

`/Workspace/Blazor/artifacts/packages/Debug/Shipping/Microsoft.AspNetCore.Blazor.Mono.0.10.0-dev.nupkg`

#### AspNetCore

Now checkout AspNetCore:

```
$ git remote -v
baulig	git@github.com:baulig/AspNetCore.git (fetch)
baulig	git@github.com:baulig/AspNetCore.git (push)
origin	https://github.com/aspnet/AspNetCore.git (fetch)
origin	https://github.com/aspnet/AspNetCore.git (push)
```

Edit `eng/Versions.prop`:

```
diff --git a/eng/Versions.props b/eng/Versions.props
index e3fa0ec0f1..e35b2e5332 100644
--- a/eng/Versions.props
+++ b/eng/Versions.props
@@ -82,7 +82,7 @@
     <!-- Only listed explicitly to workaround https://github.com/dotnet/cli/issues/10528 -->
     <MicrosoftNETCorePlatformsPackageVersion>3.0.0-preview7.19312.3</MicrosoftNETCorePlatformsPackageVersion>
     <!-- Packages from aspnet/Blazor -->
-    <MicrosoftAspNetCoreBlazorMonoPackageVersion>0.10.0-preview7.19317.1</MicrosoftAspNetCoreBlazorMonoPackageVersion>
+    <MicrosoftAspNetCoreBlazorMonoPackageVersion>0.10.0-dev</MicrosoftAspNetCoreBlazorMonoPackageVersion>
     <!-- Packages from aspnet/Extensions -->
     <InternalAspNetCoreAnalyzersPackageVersion>3.0.0-preview7.19312.4</InternalAspNetCoreAnalyzersPackageVersion>
     <MicrosoftAspNetCoreAnalyzerTestingPackageVersion>3.0.0-preview7.19312.4</MicrosoftAspNetCoreAnalyzerTestingPackageVersion>
@@ -260,6 +260,7 @@
     <RestoreSources>
       $(RestoreSources);
       https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
+      /Workspace/Blazor/artifacts/packages/Debug/Shipping;
     </RestoreSources>
     <!-- In an orchestrated build, this may be overriden to other Azure feeds. -->
     <DotNetAssetRootUrl Condition="'$(DotNetAssetRootUrl)'==''">https://dotnetcli.blob.core.windows.net/dotnet/</DotNetAssetRootUrl>
```

Do a `./build.sh` and `./build.sh --pack` and get a coffee while it's running, the build will take approximately 9 minutes.

The `./build.sh --pack` might fail with

```
/Workspace/AspNetCore/.dotnet/sdk/3.0.100-preview5-011568/Sdks/NuGet.Build.Tasks.Pack/build/NuGet.Build.Tasks.Pack.targets(199,5): error : Could not find a part of the path '/Workspace/AspNetCore/src/Components/Browser.JS/dist/Debug'. [/Workspace/AspNetCore/src/Components/Blazor/Build/src/Microsoft.AspNetCore.Blazor.Build.csproj]
    0 Warning(s)
    1 Error(s)

```

Go to `src/Components` and run `./build.sh` there (that should take about three minutes).  Then go to the root directory and run the `./build.sh --pack` again (should take about six minutes).

