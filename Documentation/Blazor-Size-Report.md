# Initial Size Report - Empty Blazor

## Getting Started

First, you need to checkout and build Mono, then set the `MONO_ROOT` environment
variable to the path of your checkout.

For instance, I have my Mono checked out in `/Workspace/mono-linker` and I did

    $ ./autogen.sh --prefix=/Workspace/LINKER --with-runtime-preset=all && make -j18 && make install
    $ export MONO_ROOT=/Workspace/mono-linker

Note that the tests will use the _compiled_ version of that Mono (so for instance `/Workspace/mono-linker/mcs/class/lib/wasm`), not the installed one.

I checked out this module at `/Workspace/linker-optimizer`.

## Empty Blazor Sample

There's a simple "Hello World" sample in `Tests/Blazor/EmptyBlazor`.

## Running without the Optimizer

Go to `Tests/Blazor`, then do

    make EXTRA_OPTIMIZER_ARGS='--optimizer-options disable-module' V=1

You should see something like this:

```
/Library/Developer/CommandLineTools/usr/bin/make -C /Workspace/linker-optimizer standalone-build
msbuild /nologo /verbosity:quiet /Workspace/linker-optimizer/Mono.Linker.Optimizer.sln
Running test EmptyBlazor
(cd EmptyBlazor && dotnet build)
Microsoft (R) Build Engine version 16.2.0-preview-19278-01+d635043bd for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 98.23 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
/usr/local/share/dotnet/sdk/3.0.100-preview6-012266/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(158,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  Writing boot data to: /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/blazor.boot.json
  Blazor Build result -> 34 files in /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/dist

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.58
(cd EmptyBlazor && dotnet publish)
Microsoft (R) Build Engine version 16.2.0-preview-19278-01+d635043bd for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 16.04 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
/usr/local/share/dotnet/sdk/3.0.100-preview6-012266/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(158,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  Blazor Build result -> 34 files in /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/dist
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/publish/
MONO_PATH=/Workspace/mono-linker/mcs/class/lib/build /Workspace/mono-linker/runtime/mono-wrapper  --debug /Workspace/linker-optimizer/output/bin/Debug/Mono.Linker.Optimizer.exe --optimizer EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/EmptyBlazor.dll --optimizer-xml EmptyBlazor/optimizer.xml --optimizer-report output/martin-report.xml --optimizer-options report-profile=wasm,report-mode=actions+size+detailed --optimizer-options disable-module  -out output -b true -c link -l none --dump-dependencies -d /Workspace/mono-linker/mcs/class/lib/wasm
Reading XML description from EmptyBlazor/optimizer.xml.
Reading XML description from /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/../../Corlib/corlib-api.xml.
Optimizer is disabled.
Mono Linker Optimizer finished in 00:00:02.9358177.
```

It will generate the following output:

```
-rw-r--r--   1 martin  wheel     4608 Jun 20 21:26 EmptyBlazor.dll
-rw-r--r--   1 martin  wheel      492 Jun 20 21:26 EmptyBlazor.pdb
-rw-r--r--   1 martin  wheel    19968 Jun 20 21:26 Microsoft.AspNetCore.Blazor.dll
-rw-r--r--   1 martin  wheel     7680 Jun 20 21:26 Microsoft.AspNetCore.Components.Browser.dll
-rw-r--r--   1 martin  wheel    30720 Jun 20 21:26 Microsoft.AspNetCore.Components.dll
-rw-r--r--   1 martin  wheel    12800 Jun 20 21:26 Microsoft.Extensions.DependencyInjection.Abstractions.dll
-rw-r--r--   1 martin  wheel    51712 Jun 20 21:26 Microsoft.Extensions.DependencyInjection.dll
-rw-r--r--   1 martin  wheel    30208 Jun 20 21:26 Microsoft.JSInterop.dll
-rw-r--r--   1 martin  wheel     8192 Jun 20 21:26 Mono.Security.dll
-rw-r--r--   1 martin  wheel     3572 Jun 20 21:26 Mono.Security.pdb
-rw-r--r--   1 martin  wheel     5120 Jun 20 21:26 Mono.WebAssembly.Interop.dll
-rw-r--r--   1 martin  wheel   287744 Jun 20 21:26 System.Core.dll
-rw-r--r--   1 martin  wheel   135172 Jun 20 21:26 System.Core.pdb
-rw-r--r--   1 martin  wheel    10752 Jun 20 21:26 System.Net.Http.dll
-rw-r--r--   1 martin  wheel     3584 Jun 20 21:26 System.Net.Http.pdb
-rw-r--r--   1 martin  wheel   102912 Jun 20 21:26 System.dll
-rw-r--r--   1 martin  wheel    40780 Jun 20 21:26 System.pdb
-rw-r--r--   1 martin  wheel  1535547 Jun 20 21:26 linker-dependencies.xml.gz
-rw-r--r--   1 martin  wheel  1221632 Jun 20 21:26 mscorlib.dll
-rw-r--r--   1 martin  wheel   582224 Jun 20 21:26 mscorlib.pdb
```

## Running with the Optimizer enabled

Simply type `make`.

```
/Library/Developer/CommandLineTools/usr/bin/make -C /Workspace/linker-optimizer standalone-build
msbuild /nologo /verbosity:quiet /Workspace/linker-optimizer/Mono.Linker.Optimizer.sln
Running test EmptyBlazor
(cd EmptyBlazor && dotnet build)
Microsoft (R) Build Engine version 16.2.0-preview-19278-01+d635043bd for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 86.88 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
/usr/local/share/dotnet/sdk/3.0.100-preview6-012266/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(158,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  Writing boot data to: /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/blazor.boot.json
  Blazor Build result -> 34 files in /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/dist

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.34
(cd EmptyBlazor && dotnet publish)
Microsoft (R) Build Engine version 16.2.0-preview-19278-01+d635043bd for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 13.4 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
/usr/local/share/dotnet/sdk/3.0.100-preview6-012266/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(158,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  Blazor Build result -> 34 files in /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/dist
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/publish/
Reading XML description from EmptyBlazor/optimizer.xml.
Reading XML description from /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/../../Corlib/corlib-api.xml.
Initializing Mono Linker Optimizer.
Preprocessor mode: Automatic.
BB SCAN FAILED: System.Boolean Microsoft.Extensions.Internal.ParameterDefaultValue::TryGetDefaultValue(System.Reflection.ParameterInfo,System.Object&)
BB SCAN FAILED: System.Boolean System.Linq.Expressions.Compiler.CompilerScope/<GetVariablesIncludingMerged>d__37::MoveNext()
BB SCAN FAILED: System.Object Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver::VisitConstructor(Microsoft.Extensions.DependencyInjection.ServiceLookup.ConstructorCallSite,Microsoft.Extensions.DependencyInjection.ServiceLookup.RuntimeResolverContext)
BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Reflection.MemberAssignment/<GetPropertiesIncludingInherited>d__0::MoveNext()
Size check: Microsoft.AspNetCore.Blazor, actual=19968, expected=19968 (tolerance 1%)
Size check: Microsoft.Extensions.DependencyInjection.Abstractions, actual=12800, expected=12800 (tolerance 1%)
Size check: Microsoft.AspNetCore.Components, actual=30720, expected=30720 (tolerance 1%)
Size check: mscorlib, actual=1197056, expected=1197056 (tolerance 1%)
Size check: System.Core, actual=287744, expected=287744 (tolerance 1%)
Size check: System, actual=102912, expected=102912 (tolerance 1%)
Size check: System.Net.Http, actual=10752, expected=10752 (tolerance 1%)
Size check: Mono.Security, actual=8192, expected=8192 (tolerance 1%)
Size check: Mono.WebAssembly.Interop, actual=5120, expected=5120 (tolerance 1%)
Size check: Microsoft.AspNetCore.Components.Browser, actual=7680, expected=7680 (tolerance 1%)
Size check: Microsoft.JSInterop, actual=29696, expected=29696 (tolerance 1%)
Size check: Microsoft.Extensions.DependencyInjection, actual=51712, expected=51712 (tolerance 1%)
Mono Linker Optimizer finished in 00:00:05.3027995.
```

and the output is

```
-rw-r--r--   1 martin  wheel     4608 Jun 20 21:28 EmptyBlazor.dll
-rw-r--r--   1 martin  wheel      492 Jun 20 21:28 EmptyBlazor.pdb
-rw-r--r--   1 martin  wheel    19968 Jun 20 21:28 Microsoft.AspNetCore.Blazor.dll
-rw-r--r--   1 martin  wheel     7680 Jun 20 21:28 Microsoft.AspNetCore.Components.Browser.dll
-rw-r--r--   1 martin  wheel    30720 Jun 20 21:28 Microsoft.AspNetCore.Components.dll
-rw-r--r--   1 martin  wheel    12800 Jun 20 21:28 Microsoft.Extensions.DependencyInjection.Abstractions.dll
-rw-r--r--   1 martin  wheel    51712 Jun 20 21:28 Microsoft.Extensions.DependencyInjection.dll
-rw-r--r--   1 martin  wheel    29696 Jun 20 21:28 Microsoft.JSInterop.dll
-rw-r--r--   1 martin  wheel     8192 Jun 20 21:28 Mono.Security.dll
-rw-r--r--   1 martin  wheel     3572 Jun 20 21:28 Mono.Security.pdb
-rw-r--r--   1 martin  wheel     5120 Jun 20 21:28 Mono.WebAssembly.Interop.dll
-rw-r--r--   1 martin  wheel   287744 Jun 20 21:28 System.Core.dll
-rw-r--r--   1 martin  wheel   135172 Jun 20 21:28 System.Core.pdb
-rw-r--r--   1 martin  wheel    10752 Jun 20 21:28 System.Net.Http.dll
-rw-r--r--   1 martin  wheel     3584 Jun 20 21:28 System.Net.Http.pdb
-rw-r--r--   1 martin  wheel   102912 Jun 20 21:28 System.dll
-rw-r--r--   1 martin  wheel    40780 Jun 20 21:28 System.pdb
-rw-r--r--   1 martin  wheel  1523305 Jun 20 21:28 linker-dependencies.xml.gz
-rw-r--r--   1 martin  wheel    66934 Jun 20 21:28 martin-report.xml
-rw-r--r--   1 martin  wheel  1197056 Jun 20 21:28 mscorlib.dll
-rw-r--r--   1 martin  wheel   571020 Jun 20 21:28 mscorlib.pdb
```

As you can see, it is almost identical in size.

## A few words about the size report

A detailed size report will be generated in `output/martin-report.xml`.

When you look at the file, you will see things such as

```
		<assembly name="EmptyBlazor" size="4608" code-size="93">
			<namespace name="EmptyBlazor" size="93">
				<type name="Program" size="93" />
			</namespace>
		</assembly>
```

On the `<assembly>` element you will see two size attributes:

- `size` is the total size of the assembly on disk
- `code-size` is the computed IL size

All other elements will only have a single `size` attribute, which is the computed IL size.

This is the total size of the IL code plus some heuristics to make up for the metadata size.  The calculation is a little bit better than what the pristing linker does, but it is not perfect.  That's where the discrepancies between the two sizes comes from.

## An initial look at the data

Looking at the biggest assemblies, there is

```
-rw-r--r--  1 martin  wheel    19968 Jun 20 21:28 output/Microsoft.AspNetCore.Blazor.dll
-rw-r--r--  1 martin  wheel    30720 Jun 20 21:28 output/Microsoft.AspNetCore.Components.dll
-rw-r--r--  1 martin  wheel    12800 Jun 20 21:28 output/Microsoft.Extensions.DependencyInjection.Abstractions.dll
-rw-r--r--  1 martin  wheel    51712 Jun 20 21:28 output/Microsoft.Extensions.DependencyInjection.dll
-rw-r--r--  1 martin  wheel    29696 Jun 20 21:28 output/Microsoft.JSInterop.dll
-rw-r--r--  1 martin  wheel   287744 Jun 20 21:28 output/System.Core.dll
-rw-r--r--  1 martin  wheel    10752 Jun 20 21:28 output/System.Net.Http.dll
-rw-r--r--  1 martin  wheel   102912 Jun 20 21:28 output/System.dll
-rw-r--r--  1 martin  wheel  1197056 Jun 20 21:28 output/mscorlib.dll
```

From the `output/martin-report.xml`, the `<assembly>` element for corlib is

```
<assembly name="mscorlib" size="1197056" code-size="813166">
```

Biggest namespaces in there are:

- `System:` 274876
- `System.Globalization`: 66472
- `System.Reflection.Emit`: 65314
- `System.Text`: 58617
- `System.Reflection`: 52171
- `System.IO`: 37081
- `System.Runtime.Serialization.Formatters.Binary`: 36576
- `System.Threading`: 28051
- `System.Collections.Generic`: 24729
- `System.Threading.Tasks`: 23360
- `System.Runtime.Serialization`: 21430
- `System.Resources`: 18204
- `System.Numerics`: 17773
- `System.Security.Cryptography`: 12964
- `Mono.Security.Cryptography`: 10856
- `System.Runtime.CompilerServices`: 9436

This sample is configured (via `./Tests/Blazor/EmptyBlazor/optimizer.xml`) to disable everything, but as you can see above, this includes several namespaces that we have previously removed.

## Deep Dive: Reflection Emit

Now let's have a look where `System.Reflection.Emit` comes from.  To do so, we edit the `Tests/Blazor/EmptyBlazor/optimizer.xml` configuration file and add

   	<type fullname="System.Reflection.Emit.TypeBuilder" action="warn" />

(you can also use `action="fail"` to make it hard fail)

If you type `make` and look at the build output, you'll see tons of

```
Found fail-listed type `System.Reflection.Emit.TypeBuilder`:
  [Type System.Reflection.Emit.TypeBuilder FullName Warn]
Dependency Stack:
  MemberRef:System.Reflection.Emit.ConstructorBuilder System.Reflection.Emit.TypeBuilder::DefineConstructor(System.Reflection.MethodAttributes,System.Reflection.CallingConventions,System.Type[])
  Method:System.Type System.Linq.Expressions.Compiler.DelegateHelpers::MakeNewCustomDelegate(System.Type[])
  Method:System.Type System.Linq.Expressions.Compiler.DelegateHelpers::MakeNewCustomDelegate(System.Type[])
  Other:Mono.Linker.Optimizer.ConditionalMarkStep
```

as well as

```
Found fail-listed type `System.Reflection.Emit.TypeBuilder`:
  [Type System.Reflection.Emit.TypeBuilder FullName Warn]
Dependency Stack:
  Method:System.Reflection.Emit.TypeBuilder System.Linq.Expressions.Compiler.AssemblyGen::DefineDelegateType(System.String)
  Method:System.Reflection.Emit.TypeBuilder System.Linq.Expressions.Compiler.AssemblyGen::DefineDelegateType(System.String)
  Other:Mono.Linker.Optimizer.ConditionalMarkStep
```

So it's `System.Linq.Expressions` that's pulling that in.

## Debugging

You can edit the `Mono.Linker.Optimizer.csproj.user` to contain the following

```
    <StartArguments> --optimizer EmptyBlazor/bin/Debug/netstandard2.0/publish/EmptyBlazor/dist/_framework/_bin/EmptyBlazor.dll --optimizer-xml EmptyBlazor/optimizer.xml --optimizer-report output/martin-report.xml --optimizer-options report-profile=dotnet,report-mode=actions+size+detailed -d /Workspace/mono-linker/mcs/class/lib/wasm -out output -b true -c link -l none --dump-dependencies</StartArguments>
    <StartWorkingDirectory>Tests\Blazor</StartWorkingDirectory>
```

then open the `Mono.Linker.Optimizer.sln` is VSMac to run the same thing in the debugger.

To investigate where that `System.Linq.Expressions.Compiler.DelegateHelpers` type came from, simply add a fail-list enty for it:

```
<type fullname="System.Linq.Expressions.Compiler.DelegateHelpers" action="fail" />
```

and you'll get

```
Found fail-listed type `System.Linq.Expressions.Compiler.DelegateHelpers`:
  [Type System.Linq.Expressions.Compiler.DelegateHelpers FullName Fail]
Dependency Stack:
  Method:System.Type System.Linq.Expressions.Compiler.DelegateHelpers::MakeDelegateType(System.Type[])
  Method:System.Linq.Expressions.LambdaExpression System.Linq.Expressions.Expression::Lambda(System.Linq.Expressions.Expression,System.String,System.Boolean,System.Collections.Generic.IEnumerable`1<System.Linq.Expressions.ParameterExpression>)
  Method:System.Linq.Expressions.LambdaExpression System.Linq.Expressions.Expression::Lambda(System.Linq.Expressions.Expression,System.String,System.Boolean,System.Collections.Generic.IEnumerable`1<System.Linq.Expressions.ParameterExpression>)
  Other:Mono.Linker.Optimizer.ConditionalMarkStep
```

You can also add entire namespaces, such as for instance

```
<namespace name="System.Security.Cryptography" action="fail" />
```

## Initial Findings

I've spent some time playing around with this and here are my initial findings.

### mscorlib

- `System.Reflection.Emit` is pulled in via `System.Linq.Expressions`

- The Crypto Stack is pulled in via `System.Net.Http.HttpClientHandler`

- `System.Text` is full of encoding classes; here is the full type list:

```
UTF8Encoding
StringBuilder
UnicodeEncoding
UTF32Encoding
ASCIIEncoding
Encoding
Normalization
DecoderNLS
ValueStringBuilder
EncoderNLS
InternalEncoderBestFitFallbackBuffer
Decoder
InternalDecoderBestFitFallbackBuffer
EncoderReplacementFallbackBuffer
EncoderFallbackBuffer
Encoder
DecoderFallbackBuffer
DecoderReplacementFallback
EncoderReplacementFallback
EncoderFallbackException
DecoderReplacementFallbackBuffer
EncoderExceptionFallbackBuffer
EncodingHelper
ValueUtf8Converter
InternalDecoderBestFitFallback
DecoderExceptionFallbackBuffer
DecoderFallback
InternalEncoderBestFitFallback
EncoderFallback
DecoderFallbackException
StringBuilderCache
EncoderExceptionFallback
DecoderExceptionFallback
NormalizationForm
NormalizationCheck
```

- `System.Numerics` is essentially just the `Vector<T>` class`.

- `System.Runtime.CompilerServices` is full of custom attribute classes that could possibly be removed.

- In `System.Globalization`, the calendars that we removed are actually removed.

### System.Core

- assembly size is 287744, code size 196591
- `System.Linq.Expressions` has a code size of 108272
- second largest namespace is `System.Linq.Expressions.Compiler` with a code size of 62288.
  - it is pulled in via `System.Linq.Expressions.Expression.Lambda()` and `System.Linq.Expressions.Expression<T>.Compile()`

### System

It's essentially just `System.Uri` and some of the certificate / crypto stack.

Although tiny in size, I see both `LegacyTlsProvider` and `MonoBtlsProvider`.

### Sytem.Net.Http

It's just the very basic classes:

```
HttpClient
HttpUtilities
HttpMessageInvoker
HttpClientHandler
HttpMessageHandler
NetEventSource
```

### Others

The `Microsoft.Extensions.DependencyInjection` assembly seems quite large, it's 51712 on disk, code size 24531.

# Conclusion

This is just a very simple first look that I did in one single afternoon and not a thorough analysis.
