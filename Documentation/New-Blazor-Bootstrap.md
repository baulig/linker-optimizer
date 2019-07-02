# New Bootstrap

## Initial Setup

### Create Workspace

Using `/Workspace/Blazor` as root directory.

Checkout `aspnet/Blazor`:

```
$ pwd && git remote -v && git branch -vv
/Workspace/Blazor/Blazor
baulig	git@github.com:baulig/Blazor.git (fetch)
baulig	git@github.com:baulig/Blazor.git (push)
origin	https://github.com/aspnet/Blazor.git (fetch)
origin	https://github.com/aspnet/Blazor.git (push)
* martins-playground eae050d [baulig/martins-playground] X
  master             cce6ca2 [origin/master] Update branding to preview 8
```

Checkout `aspnet/Extensions`:

```
$ pwd && git remote -v && git branch -vv
/Workspace/Blazor/Extensions
baulig	git@github.com:baulig/Extensions.git (fetch)
baulig	git@github.com:baulig/Extensions.git (push)
origin	https://github.com/aspnet/Extensions.git (fetch)
origin	https://github.com/aspnet/Extensions.git (push)
* martins-playground 8c6d69570 [baulig/martins-playground] [master] Update dependencies from dotnet/core-setup (#1888)
  master             416befdba [origin/master] Update branding to preview 8
```

Checkout `aspnet/AspNetCore`:

```
$ !pw
pwd && git remote -v && git branch -vv
/Workspace/Blazor/AspNetCore
baulig	git@github.com:baulig/AspNetCore.git (fetch)
baulig	git@github.com:baulig/AspNetCore.git (push)
origin	https://github.com/aspnet/AspNetCore.git (fetch)
origin	https://github.com/aspnet/AspNetCore.git (push)
* martins-playground bd1f9254d8 [baulig/martins-playground] X
  master             7344c1ea8a [origin/master] Update branding to preview 8
```

Make sure you have the latest version, which contains Larry's initialization changes:
https://github.com/baulig/AspNetCore/commit/d0d19d078822de94888f602bdfc4c67f304cd210.

Install the latest .NET Dogfood SDK.

```
$ dotnet --info
.NET Core SDK (reflecting any global.json):
 Version:   3.0.100-preview7-012635
 Commit:    cd5572d30b

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  10.14
 OS Platform: Darwin
 RID:         osx.10.14-x64
 Base Path:   /usr/local/share/dotnet/sdk/3.0.100-preview7-012635/

Host (useful for support):
  Version: 3.0.0-preview7-27826-04
  Commit:  5c4d829254

.NET Core SDKs installed:
  2.0.3 [/usr/local/share/dotnet/sdk]
  2.1.202 [/usr/local/share/dotnet/sdk]
  2.2.203 [/usr/local/share/dotnet/sdk]
  2.2.300 [/usr/local/share/dotnet/sdk]
  2.2.301-preview-010200 [/usr/local/share/dotnet/sdk]
  3.0.100-preview4-010713 [/usr/local/share/dotnet/sdk]
  3.0.100-preview6-012266 [/usr/local/share/dotnet/sdk]
  3.0.100-preview7-012629 [/usr/local/share/dotnet/sdk]
  3.0.100-preview7-012635 [/usr/local/share/dotnet/sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.2.1 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview6.19307.2 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview7.19325.7 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview7.19325.8 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 2.0.3 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.9 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview4-27511-06 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview6-27813-07 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview7-27826-04 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]

```

### Build

To build this, first build the `Blazor` module, then `Extensions`, then `AspNetCore`.

```
(cd Blazor && ./build.sh)
(cd Extensions && ./build.sh)
(cd AspNetCore && ./build.sh)
(cd AspNetCore/src/Components && ./build.sh)
(cd AspNetCore/src/Components && ./build.sh --pack)
(cd AspNetCore && ./build.sh --pack)
```

Then build the sample:

```
$ pwd
/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor
$ git reset --hard HEAD
$ git clean -xffd
$ dotnet build
$ dotnet publish
```

Edit `bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/blazor.webassembly.js`, search for `createEmscriptenModuleInstance()` in it and comment out

```
//        MONO.mono_wasm_set_runtime_options(["--trace"]);
```

Or otherwise your web browser will really love you.

**WARNING**: If leave the `--trace` in and you're doing this on a Mac with Firefox and an external monitor, this _may_ cause the white flicker of death where you have to log out and in again!  If've seen this happening this afternoon.

You can either do `dotnet run` or manually start a server:

```
http-server ./EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/
```

### Preparing the Linker Optimizer

First, checkout and build Mono normally.  Do not use any of the SDK stuff, that's not going to work, and also don't worry about runtime presets.  Just build normally.

```
cd /Workspace/mono-linker
git clean -xffd && git submodule foreach git clean -xffd
./autogen.sh --prefix=/Workspace/LINKER && make -j18 && make install
```

(or if you feel lucky want to stress-test your computer, try `make -j` ...)

Then build the `wasm` profile:

```
cd mcs/class
make PROFILE=wasm -j
```

No need to do `make install` - and you could actually get away with just compiling _some_ of the assemblies, but I find it a lot more tedious to go back and add what I was missing than to just building them all.

Now set the `MONO_ROOT` environment variable to the Mono _checkout_ that you just built (not it's install location).

```
$ export MONO_ROOT=/Workspace/mono-linker
```

Then type `make` in the top-level directory:

```
$ pwd
/Workspace/linker-optimizer
$ make
msbuild /nologo /verbosity:quiet /Workspace/linker-optimizer/Mono.Linker.Optimizer.sln
```

Okay, so much about preparations.

### Using the Linker Optimizer

After you've done all the above mentioned preparations, it's time to actually use the Linker Optimizer.

Go to `Tests/Blazor`:

```
$ pwd
/Workspace/linker-optimizer/Tests/Blazor
```

The latest version of that `Makefile` has a few targets and it's still kinda experimental.

Do not use any of the `build-*` targets for now as they'd wipe out and rebuild the Blazor sample.

Instead, use `dotnet build` and `dotnet prepare`.

Then you can use the `link-EmptyBlazor` target:

```
$ make V=1 link-EmptyBlazor
MONO_PATH=/Workspace/mono-linker/mcs/class/lib/build /Workspace/mono-linker/runtime/mono-wrapper  --debug /Workspace/linker-optimizer/output/bin/Debug/Mono.Linker.Optimizer.exe --optimizer EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll --optimizer-xml EmptyBlazor/optimizer.xml --optimizer-report output/martin-report.xml --optimizer-options report-profile=wasm,report-mode=actions+size+detailed   -out output/_framework/_bin -d EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/ -b true -c copy -u copy -p copy mscorlib -p copy System -l none --keep-facades true --verbose
Reading XML description from EmptyBlazor/optimizer.xml.
Reading XML description from /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/../../Corlib/corlib-api.xml.
Initializing Mono Linker Optimizer.
Preprocessor mode: Automatic.
BB SCAN FAILED: System.Boolean System.Threading.Tasks.ThreadPoolTaskScheduler/<FilterTasksFromWorkItems>d__6::MoveNext()
BB SCAN FAILED: System.Void System.Text.Json.JsonSerializer::ReadCore(System.Text.Json.JsonSerializerOptions,System.Text.Json.Utf8JsonReader&,System.Text.Json.ReadStack&)
BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaders/<GetHeaderStrings>d__23::MoveNext()
BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaders/<GetEnumeratorCore>d__28::MoveNext()
BB SCAN FAILED: System.Threading.Tasks.Task System.Net.Http.HttpContent::LoadIntoBufferAsync(System.Int64,System.Threading.CancellationToken)
BB SCAN FAILED: System.Object Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver::VisitConstructor(Microsoft.Extensions.DependencyInjection.ServiceLookup.ConstructorCallSite,Microsoft.Extensions.DependencyInjection.ServiceLookup.RuntimeResolverContext)
BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaderValueCollection`1/<GetEnumerator>d__21::MoveNext()
BB SCAN FAILED: System.Boolean System.Text.Json.JsonSerializer::Write(System.Text.Json.Utf8JsonWriter,System.Int32,System.Text.Json.JsonSerializerOptions,System.Text.Json.WriteStack&)
BB SCAN FAILED: System.Boolean Microsoft.Extensions.Internal.ParameterDefaultValue::TryGetDefaultValue(System.Reflection.ParameterInfo,System.Object&)
BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericTDeclaredPropertyIEnumerable>d__27::MoveNext()
BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericTRuntimePropertyIEnumerable>d__26::MoveNext()
BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericIEnumerableFromDictionary>d__28::MoveNext()
BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Reflection.MemberAssignment/<GetPropertiesIncludingInherited>d__0::MoveNext()
BB SCAN FAILED: System.Boolean System.Linq.Expressions.Compiler.CompilerScope/<GetVariablesIncludingMerged>d__37::MoveNext()
BB SCAN FAILED: System.Boolean System.Security.Claims.ClaimsIdentity/<get_Claims>d__51::MoveNext()
Output action:     Link assembly: EmptyBlazor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
Output action:     Copy assembly: netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: Microsoft.AspNetCore.Blazor, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.Extensions.DependencyInjection.Abstractions, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.AspNetCore.Components, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
Output action:     Copy assembly: System.Core, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
Output action:     Copy assembly: System, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
Output action:     Copy assembly: System.Data, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
Output action:     Copy assembly: System.Drawing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: System.IO.Compression, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
Output action:     Copy assembly: System.IO.Compression.FileSystem, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
Output action:     Copy assembly: System.ComponentModel.Composition, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
Output action:     Copy assembly: System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
Output action:     Copy assembly: System.Numerics, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
Output action:     Copy assembly: System.Runtime.Serialization, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
Output action:     Copy assembly: System.Transactions, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
Output action:     Copy assembly: System.Web.Services, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
Output action:     Copy assembly: System.Xml, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
Output action:     Copy assembly: System.Xml.Linq, Version=2.0.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
Output action:     Copy assembly: Mono.Security, Version=2.0.5.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756
Output action:     Copy assembly: System.ServiceModel.Internals, Version=0.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
Output action:     Copy assembly: Microsoft.Extensions.Logging.Abstractions, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Mono.WebAssembly.Interop, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.AspNetCore.Components.Browser, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.JSInterop, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.Extensions.DependencyInjection, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: System.Text.Json, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: Microsoft.Extensions.Options, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.AspNetCore.Metadata, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.AspNetCore.Authorization, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: System.ComponentModel.Annotations, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
Output action:     Copy assembly: Microsoft.Extensions.Primitives, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: System.Memory, Version=4.0.99.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: System.Runtime.CompilerServices.Unsafe, Version=4.0.4.0, Culture=neutral, PublicKeyToken=null
Output action:     Copy assembly: System.Numerics.Vectors, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
Output action:     Copy assembly: System.Threading.Tasks.Extensions, Version=4.2.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: Microsoft.Bcl.AsyncInterfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: System.Buffers, Version=4.0.99.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
Mono Linker Optimizer finished in 00:00:07.4928121.
```

This will put the outputs into the `output` directory and you should see 4 versions of `mscorlib.dll`:

```
$ find . -name mscorlib.dll
./output/_framework/_bin/mscorlib.dll
./EmptyBlazor/obj/Debug/netstandard2.0/blazor/resolvedassemblies/mscorlib.dll
./EmptyBlazor/bin/Debug/netstandard2.0/dist/_framework/_bin/mscorlib.dll
./EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/mscorlib.dll
```

Copy the generated one from `./output/_framework/_bin/mscorlib.dll` into the publish directory:

```
cp -f ./output/_framework/_bin/mscorlib.dll ./EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/
```

Then start the `http-server` and open it in your browser:

```
http-server ./EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/
```

This will work because - if you look at the output above, you'll see that none of the assemblies are actually linked.
