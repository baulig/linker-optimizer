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

The scanner also looks at the target of each `call` instruction to resolve linker conditionals (see the section about Linker Conditionals for details).  All linker conditionals will be put into a basic block or their own.

If no linker conditionals are found, then by default the scan result will be discarded and we continue with the normal linker's code-path by calling the `base.MarkMethodBody ()` method.

This behavior can be overridden by the `analyze-all` option (which is mainly intended for debugging and stress-testing the module).  As of this writing, the corlib test suite passes with `analyze-all` enabled.

Scanning all method bodies is required to detect linker conditionals in them.  Should performance be an issue, then we can use the new XML to explicitly tell the linker which classes need to be scanned.  For the moment, I wanted to keep things as simple as possible and not require explicit registration via XML.

If any linker conditionals have been found (or `analyze-all` has been given), then the additional steps will be enabled, which will be described in the following chapters.

## Linker Conditionals

There are two kinds of linker conditionals:

1. An explicit call to a static method in the `MonoLinkerSupport` class (currently in `System.Runtime.CompilerServices`, but we can move / rename it).  This class is detected in both corlib and a special assembly called `TestHelpers.dll`, which is used by the test suite.
This does not need ot be the final solution, I just simply needed something that's easy to detect just by looking at the call instruction.
2.  A call to an "implicit conditional method".  At the moment, this requires a special pre-scan step, which needs to be explicitly enabled via the `preprocess` option.  This was written _before_ the new XML code was in place; the plan is to replace it with XML-based registration.
3.  (Planned) XML-based registration of "implicit conditional methods".

### Explicit Conditional Methods

We currently support the following conditionals.  Please note that some of these have become obsoleted by newer design philosophy and functionality (namely the new fully complete flow analysis and dead code elimination).

This code was written before the dead code eliminator and with the old and obsoleted design philosophy in mind that "features" should be dynamically detectable at runtime - we now require all features to be explicitly declared with link-time options (either in XML or on the command-line).

I believe that all of them except for `IsFeatureSupported ()` can now be safely removed.

All of the following methods are `static` and have `[MethodImpl(MethodImplOptions.NoInlining)]`; I'll omit these in the following list for simplicity.

The core part is:

* `bool IsFeatureSupported (MonoLinkerFeature feature)`
* `bool ConstantCall ()` (virtual, this method does not actually exist; see next section)

The following are still in active use by the test suite (though we could remove them from corlib and only resolve from the test helper assembly):

* `bool IsTypeAvailable (string type)` (we take a `string` argument to support internal types)
* `bool IsFeatureSupported (MonoLinkerFeature feature)`
* `void RequireFeature (MonoLinkerFeature feature)` (only used in one single test)

And I belive all of those can go away:

* `bool IsWeakInstanceOf<T> (object obj)`
* `bool IsTypeAvailable<T> ()`
* `bool AsWeakInstanceOf<T> (object obj, out T instance)`

Since each of these conditionals lives in its own class, it is really easy to remove some of them.

The enum `MonoLinkerFeature` is currently defined as

```
	enum MonoLinkerFeature
	{
		Unknown, // keep this in first position, this is always false.
		Martin,
		ReflectionEmit,
		Serialization,
		Remoting,
		Globalization,
		Encoding,
		Security,
		Crypto
	}
 ```

We could in theory replace that enum with a non-enum based approach, but again this will make registration and looking a bit more complex and I wanted to keep it simple for the moment.

Adding an additional enum value is one line of code and literally takes me one minute because those names are also automatically resolved from the XML options.

We do not have to spew these all over the place, in fact they can all be contained to one single file.

Each of these features can be disabled via either the XML or the command-line, for instance

```
<features>
  <feature name="security" enabled="false" />
  <feature name="remoting" enabled="false" />
</features>
```

The `Unknown` feature is always disabled (this is used by the regression tests for flow analysis and dead code elimination).  There are also some IL tests containing `ldc.i4.0` and `ldc.i4.1` for `Unknown` and `Martin` respectively, that's why they're in first position.

We can of course remove them from corlib, but should reserve some values for those regression tests (since you have to use constant loads like `ldc.i4.1` in IL, it's kinda cumbersome to modify those enum values).

I didn't hook up any command-line options yet, but there's an environment variable `MARTIN_LINKER_OPTIONS`, so you could for instance say `MARTIN_LINKER_OPTIONS=security,remoting` to disable those two features (by default, all features are enabled).

### Implicit Conditional Methods

As I mentioned above, `bool ConditionalCall ()` does not actually exist - it is purely virtual and is used for what I call "implicit conditional methods".

An "implicit conditional method" currently has the following requirements: it must be parameterless, return `bool`, contain an explicit linker conditional and resolve to a boolean constant after dead code elimination.

Specifically, after all processing, the method body must be one of

```
ldc.i4.0
ret
```
or
```
ldc.i4.1
ret
```
or
```
ldc.i4.0
ldc.i4.0
ceq
ret
```

We could of course hook up the XML and just "teach" the linker that an arbitrary method should be treated as such.

If the `preprocess` step is enabled, then all property getters will be scanned.  It is really just a matter of how we're telling the linker which methods have such "magic" properties.

The property does not need to be static and it may contain additional code besides the conditional, such as for instance

```
public bool IsReadOnly {
    get {
        return !MonoLinkerSupport.IsFeatureSupported(MonoLinkerFeature.Globalization) || _isReadOnly;
    }
}
```

You can really put arbitrary code into that property getter (which allows the conditional to be added to existing properties!) as long as the dead code elimination component (which is actually quite advanced) can resolve it all into a boolean constant.

Once a method is identified to be such "magic" one, then all calls to it are treated as a linker conditional - the call will be put onto a basic block by itself with an instance of `ConstantCallConditional` assigned it it.

### Conditional Resolution

After basic block scanning is comnplete, all linker conditionals will be resolved.

Each such conditional will be in a basic block by itself and the `BasicBlock` will have an instance of the abstract `LinkerConditional` class assigned to it.

During resolution, that block will be rewritten to resolve the conditional; the `call` instruction will be replaced with its direct boolean result.

Since some of the conditional methods may have arguments and we're removing the `call`, we need to get rid of the arguments as well.  Here, we distinguish two situations:

* if the instruction immediately preceding the `call` is a simple load (such as `ldloc.1`, `ldarg.2`, `ldc.i4.4`, `dup` etc.) we can just simply remove that load instruction.
* otherwise, we insert a `pop` (as the argument it too complex for us to resolve).

This mean that this mechanism will work with arbitrarily complex arguments, but only those which will use simple loads in IL will fully benefit from dead code elimination.  If you push something complex onto the stack, that something will stay - followed by a `pop`.

The basic block scanner already does this distinction and will put such simple loads into the conditional's basic block.

Similarly, the `call` instruction may also be optionally followed by a `brfalse`, `brtrue` or `ret`.  These will also be put into the conditional's basic block.  These two conditional branches will be directly resolved into either an unconditional `br` or no branch at all, depending on the condition.

If anything else follows the `call`, then a boolean constant will be loaded onto the stack.

So again, the IL can be arbitrarily complex, but only the simple cases will be treated specially.  If the IL is too complex, then worst case the linker won't be able to detect some basic blocks as being "dead" - but it won't break, you'd still get the correct boolean value, just that value won't say "Hey, I'm a constant!".

## Flow Analysis

### Preliminary Thoughts

After all linker conditionals are resolved, flow analysis will be performed.

Just having those conditionals alone won't do us any good - sure, the code would be skipped at runtime, but as long as it's still in the IL, everything it references must be kept in the IL as well.  And you can easily pull in "the entire world" if you're not careful!

The basic idea is that instead of explicitly declaring a bunch of types and methods as conditional either in XML or by some other means, we would like to have as much automation as possible.  And the end result should as closely resemble what you would get had the linker conditional been replaced by a hard `#if` that's been resolved by the compiler.

Or in other words - if you replaced every single `MonoLinkerSupport.IsFeatureSupported ()` (and everything that calls it) with `#if` conditionals - the compiled output would be roughly equivalent to the linked output that you'd get from the automation.

And those conditionals should be in as few places as possible to make most use of the automation.

Ideally, this will also provide us with a fallback-option because should this linker research project either fail or not finish in time, then we could still easily go back to compiler-based hard-conditionals.

The main advantage of the automation is that it will allow our customers to selectively enable certain features for some of their projects.  And the idea is to make this as easy as by the click of a button - you want globalization, non-western encodings, crypto, etc. - check this box and you got it.  You don't need any of those?  Well, here's your decreased code size.

To do any of this, we need flow analysis.

### The Code

This is actually the second implementation of the flow analysis code, a complete rewrite over the first version.

And quite surprisingly, the actual implementation is actually quite small - as of this writing, the entire class is actually less than 150 lines of code.

This is because the Basic Block Scanner already takes care of most of the work by computing and keeping track of Branch Types and Jump Origins.

The flow analysis code also does not distinguish between "definitely reachable" and "conditionally reachable" - it doesn't need to, all that matters is whether or not a basic block could possibly be reached.  And it also doesn't track variable assignments, lifetime or anything thelike.  Again, because it doesn't need to.

### Deep Dive

The algorithm is actually quite simple.

We already have Branch Types and Jump Origins.  The Branch Type determines whether or not the next block will be reachable (like for instance if you encounter a `BranchType.Return`, control can never fall over from the current block to the next).

> All `finally` blocks are assumed to be always reachable unless the entire exception block will be removed.

One important thing I learned while playing around this this code is that for our set purposes, we can actually make one very important assumption - an assumption which will simplify the algorithm quite significatly!

> Once a basic block has been marked as definitely reachable, it will never change that state.

And, as mentioned above, the second and equally important optimization is

> We do not distinguish between "definitely reachable" and "optionally reachable".

So now let's dive into the algorithm.

While iterating over all the basic blocks, we know whether or not control can flow over from the previous block.  We maintain a simple `reachable` status for that.

Then, we have to look at our Jump Origins.  If we've already marked the origin block as reachable, then we can immediately mark the current block as reachable as well.  Otherwise, we remember that jump origin on an `unresolved` list.

If the current block is considered to be reachable, then once we looked at all our Jump Origins, we also walk that list of `unresolved` origins.  Do _we_ jump _to_ any of those unresolved origins?  If we do, then (because we are reachable) that origin will be reachable as well.  So we mark it as reachable and then restart our block iteration at that block - since that block's status has just changed into "reachable", other blocks between it and our current block may do so as well.

And that is already the entire algorithm.



