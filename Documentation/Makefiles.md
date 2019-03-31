The Makefiles are setup in a way to allow this module to be used either stand-alone or
as part of a Mono checkout.

In either case, this module requires a fully built Mono tree.  So to use it stand-alone,
you need to checkout Mono somewhere and do a full build.

To use this module in stand-alone mode, you need to set the `MONO_ROOT` environment
variable to the root of your checked out and built Mono workspace (make sure to point
it to your checkout, not to where you installed that Mono).

Standalone mode allows this module to easily be used with different versions corlib.

