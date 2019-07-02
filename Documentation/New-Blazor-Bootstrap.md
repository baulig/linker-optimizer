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

### Let's actually do something

Okay, so now that we've got the basics covered, let's actually do something.

The above mentioned mechanic of just copying everything is controlled by the `Makefile`, so let's open it in an editor and change that.

Near the top, there is

```
LINKER_ASSEMBLY_ARGS := -b true -c copy -u copy -p copy mscorlib -p copy System -l none --keep-facades true --verbose
```

Let's change that into

```
LINKER_ASSEMBLY_ARGS := -b true -c copy -u copy -p link mscorlib -p copy System -l none --keep-facades true --verbose
```

And return the tool:

```
$ rm -rf output/
$ make V=1 link-EmptyBlazor
MONO_PATH=/Workspace/mono-linker/mcs/class/lib/build /Workspace/mono-linker/runtime/mono-wrapper  --debug /Workspace/linker-optimizer/output/bin/Debug/Mono.Linker.Optimizer.exe --optimizer EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll --optimizer-xml EmptyBlazor/optimizer.xml --optimizer-report output/martin-report.xml --optimizer-options report-profile=wasm,report-mode=actions+size+detailed   -out output/_framework/_bin -d EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/ -b true -c copy -u copy -p link mscorlib -p copy System -l none --keep-facades true --verbose
Reading XML description from EmptyBlazor/optimizer.xml.
Reading XML description from /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/../../Corlib/corlib-api.xml.
Processing embedded resource linker descriptor: mscorlib.xml
Initializing Mono Linker Optimizer.
Preprocessor mode: Automatic.
Type Mono.ValueTuple has no fields to preserve
Type System.Reflection.Assembly has no fields to preserve
BB SCAN FAILED: System.Void System.Text.Json.JsonSerializer::ReadCore(System.Text.Json.JsonSerializerOptions,System.Text.Json.Utf8JsonReader&,System.Text.Json.ReadStack&)
BB SCAN FAILED: System.Object Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver::VisitConstructor(Microsoft.Extensions.DependencyInjection.ServiceLookup.ConstructorCallSite,Microsoft.Extensions.DependencyInjection.ServiceLookup.RuntimeResolverContext)
BB SCAN FAILED: System.Boolean System.Text.Json.JsonSerializer::Write(System.Text.Json.Utf8JsonWriter,System.Int32,System.Text.Json.JsonSerializerOptions,System.Text.Json.WriteStack&)
BB SCAN FAILED: System.Boolean Microsoft.Extensions.Internal.ParameterDefaultValue::TryGetDefaultValue(System.Reflection.ParameterInfo,System.Object&)
BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Reflection.MemberAssignment/<GetPropertiesIncludingInherited>d__0::MoveNext()
BB SCAN FAILED: System.Boolean System.Linq.Expressions.Compiler.CompilerScope/<GetVariablesIncludingMerged>d__37::MoveNext()
BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericTRuntimePropertyIEnumerable>d__26::MoveNext()
BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericTDeclaredPropertyIEnumerable>d__27::MoveNext()
BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericIEnumerableFromDictionary>d__28::MoveNext()
BB SCAN FAILED: System.Threading.Tasks.Task System.Net.Http.HttpContent::LoadIntoBufferAsync(System.Int64,System.Threading.CancellationToken)
BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaderValueCollection`1/<GetEnumerator>d__21::MoveNext()
BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaders/<GetHeaderStrings>d__23::MoveNext()
BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaders/<GetEnumeratorCore>d__28::MoveNext()
BB SCAN FAILED: System.Boolean System.Security.Claims.ClaimsIdentity/<get_Claims>d__51::MoveNext()
Output action:     Link assembly: EmptyBlazor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
Output action:     Copy assembly: netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
Output action:     Copy assembly: Microsoft.AspNetCore.Blazor, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.Extensions.DependencyInjection.Abstractions, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Copy assembly: Microsoft.AspNetCore.Components, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
Output action:     Link assembly: mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
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
Mono Linker Optimizer finished in 00:00:07.0757457.
```

Btw. you can ignore the "BB SCAN FAILED" warnings, it means there's some obsure stuff in that method's IL like fault blocks.

```
$ ls -la ./output/_framework/_bin/mscorlib.dll 
-rw-r--r--  1 martin  wheel  1676800 Jul  2 01:17 ./output/_framework/_bin/mscorlib.dll
```

Okay, it's a lot smaller now!

And it's also not going to work anymore.

Here's the stack trace from the web browser:

```
Uncaught (in promise) Error: System.TypeInitializationException: The type initializer for 'Microsoft.Extensions.DependencyInjection.ServiceLookup.ExpressionResolverBuilder' threw an exception. ---> System.TypeInitializationException: The type initializer for 'System.Dynamic.Utils.TypeUtils' threw an exception. ---> System.TypeLoadException: Generic type definition failed to init, due to: Could not load list of method overrides due to Method not found: void System.Collections.IEnumerator.Reset() assembly:System.Core.dll type:Iterator`1 member:(null)
  at System.Dynamic.Utils.TypeUtils..cctor () <0x23acaa8 + 0x00020> in <b9bff2dabdc64a418e2082a7fbfdf129>:0 
   --- End of inner exception stack trace ---
  at System.Linq.Expressions.Expression.Validate (System.Type type, System.Boolean allowByRef) <0x23ac1f0 + 0x0001a> in <b9bff2dabdc64a418e2082a7fbfdf129>:0 
  at System.Linq.Expressions.Expression.Parameter (System.Type type, System.String name) <0x23abf98 + 0x00008> in <b9bff2dabdc64a418e2082a7fbfdf129>:0 
  at Microsoft.Extensions.DependencyInjection.ServiceLookup.ExpressionResolverBuilder..cctor () <0x23a9558 + 0x0000a> in <5d8a4ff2e88444c696c83be3fd52ddb9>:0 
   --- End of inner exception stack trace ---
  at Microsoft.Extensions.DependencyInjection.ServiceLookup.CompiledServiceProviderEngine..ctor (System.Collections.Generic.IEnumerable`1[T] serviceDescriptors, Microsoft.Extensions.DependencyInjection.ServiceLookup.IServiceProviderEngineCallback callback) <0x2387bf8 + 0x00038> in <5d8a4ff2e88444c696c83be3fd52ddb9>:0 
  at Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine..ctor (System.Collections.Generic.IEnumerable`1[T] serviceDescriptors, Microsoft.Extensions.DependencyInjection.ServiceLookup.IServiceProviderEngineCallback callback) <0x23879e8 + 0x0000c> in <5d8a4ff2e88444c696c83be3fd52ddb9>:0 
  at Microsoft.Extensions.DependencyInjection.ServiceProvider..ctor (System.Collections.Generic.IEnumerable`1[T] serviceDescriptors, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions options) <0x2387690 + 0x0007e> in <5d8a4ff2e88444c696c83be3fd52ddb9>:0 
  at Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider (Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions options) <0x2386a88 + 0x00034> in <5d8a4ff2e88444c696c83be3fd52ddb9>:0 
  at Microsoft.Extensions.DependencyInjection.DefaultServiceProviderFactory.CreateServiceProvider (Microsoft.Extensions.DependencyInjection.IServiceCollection containerBuilder) <0x23868f0 + 0x0000c> in <5d8a4ff2e88444c696c83be3fd52ddb9>:0 
  at Microsoft.AspNetCore.Blazor.Hosting.WebAssemblyServiceFactoryAdapter`1[TContainerBuilder].CreateServiceProvider (System.Object containerBuilder) <0x2386730 + 0x0003c> in <e0a06e0c71f546239a2d5d10ca9a4a91>:0 
  at Microsoft.AspNetCore.Blazor.Hosting.WebAssemblyHostBuilder.CreateServiceProvider () <0x234e1e8 + 0x0017c> in <e0a06e0c71f546239a2d5d10ca9a4a91>:0 
  at Microsoft.AspNetCore.Blazor.Hosting.WebAssemblyHostBuilder.Build () <0x2330c98 + 0x0005a> in <e0a06e0c71f546239a2d5d10ca9a4a91>:0 
  at EmptyBlazor.Program.Main (System.String[] args) <0x22f0790 + 0x0000c> in <62e90bdb686446d6abe8df8b40d7f7cd>:0 
    at Object.callMethod (http://localhost:8080/_framework/blazor.webassembly.js:712:23)
    at Object.callEntryPoint (http://localhost:8080/_framework/blazor.webassembly.js:694:30)
    at http://localhost:8080/_framework/blazor.webassembly.js:434:30
    at step (http://localhost:8080/_framework/blazor.webassembly.js:378:23)
    at Object.next (http://localhost:8080/_framework/blazor.webassembly.js:359:53)
    at fulfilled (http://localhost:8080/_framework/blazor.webassembly.js:350:58)
```

### Fast turnaround

Rebuilding everything is really painful, but often not needed.  Once you have everything setup to use your locally built packages, it should be fairly easy.

Once you've done everything, the `mscorlib.dll` comes from `/Workspace/Blazor/Blazor/src/Microsoft.AspNetCore.Blazor.Mono/incoming/bcl/mscorlib.dll`, where it's packaged into some NuGet package which is then used by the other components.

You'll also find a file called `src/Microsoft.AspNetCore.Blazor.Mono/HowToUpgradeMono.md` in that `Blazor` module.

However, you don't need to go that full route after you've set it up once to use your local setup.

Simply built your Mono (as mentioned above), do a `dotnet build` and `dotnet publish`, then replace the `mscorlib.dll` (or any other assembly that you like) in that `./EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/` directory.

Then you should be able to use `make link-EmptyBlazor` to re-run the tool.

In that Makefile, that's currently a macro that's doing

```
define BlazorTest
build-$(1): $(1)/Program.cs $(1)/$(1).csproj standalone-build
	@echo Running test $(1)
	@rm -rf $(1)/bin $(1)/obj
	(cd $(1) && dotnet build)
	(cd $(1) && dotnet publish)
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	@cp -a $(1)/bin/Debug/netstandard2.0/publish/$(1)/dist/* $(LINKER_OUTPUT)
#	@rm -rf $(LINKER_OUTPUT)/_framework/_bin
#	@mkdir $(LINKER_OUTPUT)/_framework/_bin

test-$(1): build-$(1) link-$(1)

link-$(1):
	$(if $(V),,@) $(LINKER) --optimizer $(1)/bin/Debug/netstandard2.0/$(1).dll --optimizer-xml $(1)/optimizer.xml $(TEST_LINKER_ARGS) -out $(LINKER_OUTPUT)/_framework/_bin -d $(1)/bin/Debug/netstandard2.0/publish/$(1)/dist/_framework/_bin/ $(LINKER_ASSEMBLY_ARGS)
	@cp $(1)/bin/Debug/netstandard2.0/publish/$(1)/dist/_framework/_bin/$(1).dll $(LINKER_OUTPUT)/_framework/_bin

run-$(1): test-$(1)
	http-server $(LINKER_OUTPUT) -o
endef
```

