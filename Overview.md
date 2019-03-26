# General Overview

All new files are kept in a separate directory to keep changes to the original linker code to an absolute minimum.

As of this writing, the diff against the linker code is very tiny:

```
 linker/Linker/Driver.cs      | 7 +++++++
 linker/Linker/LinkContext.cs | 4 ++++
 linker/Linker/Tracer.cs      | 8 ++++++++
 linker/Linker/XApiReader.cs  | 5 ++++-
 4 files changed, 23 insertions(+), 1 deletion(-)
 ```

 In comparision, the newly added code is huge!

* New source files: `27 files changed, 4973 insertions(+)`
* New tests: `75 files changed, 2733 insertions(+)`

As you can see by the size of the newly added code, this is more like an additional module that's added on-top of the linker than a set of changes to the existing linker.

This new module consists of the following components:

1. Basic Block Scanner
2. Flow Analysis
3. Conditional Resolution
4. Dead Code Elimination
5. New XML based configuration

Before we dive deep into those components, let me first give you a brief overview of the new configuration.  At the moment, all the new configuration is in a separate section in the linker description file.  I will give a more detailed overview in a separate section in this file, but as a brief overview, you can do the following:

* enable / disable components of this module
* enable / disable features (see the section about conditionals for details)
* conditionally provide type / method entires
  * preserve type
  * rewrite method as `throw new PlatformNotSupportedException ()`
  * enable detailed size report for type
  * enable advanced debugging for type / method
  * print warning when type / method is encountered (intended for debugging)
  * hard fail when type / method is encountered (used by the test suite)
* some testing and debugging stuff

## Basic Block Scanner

When enabled, the new module replaces the `MarkStep` with a subclass called `ConditionalMarkStep`.

The main entry point is `MarkMethodBody ()` and we run the Basic Block Scanner on each method body.  There are a few "obscure" bodies that the scanner can't handle (such as for instance anything containing a `fault` block), but in general it's fairly robust and complete.

First, the entire method body will be broken down into basic blocks and each block assigned both a `BranchType` and a list of jump origins (that is, a list of other blocks which might possibly jump to this one).

In this regard, the `BasicBlockScanner` already does some of the foundation work for the `FlowAnalysis` component.