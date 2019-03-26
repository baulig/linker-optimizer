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
4. Code Rewriter
5. Dead Code Elimination
6. New XML based configuration

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

In this regard, the `BasicBlockScanner` already does some of the foundation work for the `FlowAnalysis` component.  The reasoning behind this is that this information will be needed by both the Code Rewriter and the Conditional Resolution.  And it also makes the Flow Analysis component a lot easier.

An important part of the Basic Block Scanner is the `BasicBlockList` class that will be populated by it.  This class contains high-level methods to manipulate basic blocks (and the instructions therein) while automatically keeping track of branch types and jump origins (automatically doing all the necessary adjustments).

This makes the higher-level code much simpler and cleaner as it doesn't have to worry about any of those low-level details.  You can easily insert / modify / delete instructions in a block and the `BasicBlockList` will automatically take care of everything for you.

We currently support the following branch types:

* `None` - not a branch
* `Jump` - unconditional branch (`br`, `br.s`, `leave`, `leave_s`)
* `Return` - return (`ret` instruction)
* `Exit` - unconditional exit from the current block, but without having an explicit target (`throw` or `rethrow`)
* `Switch` - `switch` statement
* `False` - boolean `false` conditional branch (`brfalse` or `brfalse.s`) 
* `True` - boolean `true` conditional branch (`brtrue` or `brtrue.s`)
* `Conditional` - any other conditional branch instruction
* `EndFinally` - `endfinally` instruction

The branch instruction will always be the last instruction of the block and for each branch instruction with an explicit target, the target block will have a jump origin pointing back to us.

For `try` or `catch` blocks, the flow analysis code assumes that each block that's not unreachable can possibly throw an exception.  Jump origins will be added accordingly.

The scanner also looks at the target of each `call` instruction to resolve linker conditionals (see the section about Linker Conditionals for details).

If no linker conditionals are found, then by default the scan result will be discarded and we continue with the normal linker's code-path by calling the `base.MarkMethodBody ()` method.

This behavior can be overridden by the `analyze-all` option (which is mainly intended for debugging and stress-testing the module).  As of this writing, the corlib test suite passes with `analyze-all` enabled.

Scanning all method bodies is required to detect linker conditionals in them.  Should performance be an issue, then we can use the new XML to explicitly tell the linker which classes need to be scanned.  For the moment, I wanted to keep things as simple as possible and not require explicit registration via XML.

If any linker conditionals have been found (or `analyze-all` has been given), then the additional steps will be enabled, which will be described in the following chapters.

## Linker Conditionals

