thisdir = tools/linker/Martin/Tests/Blazor

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

DOTNET = dotnet
DOTNET_RID = osx.10.14-x64

include $(ROOTDIR)/build/rules.make

WASM_PROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/wasm)

LINKER_ASSEMBLY_ARGS := \
	-b true -c copy -u copy -p link mscorlib -p link System -p link System.Core \
	-p link Microsoft.Extensions.DependencyInjection -p link EmptyBlazor \
	-p link Microsoft.AspNetCore.Components.Browser -p link Microsoft.AspNetCore.Blazor \
	-p link Microsoft.AspNetCore.Components -p link Mono.WebAssembly.Interop \
	-p link Microsoft.JSInterop -p link Microsoft.Extensions.DependencyInjection.Abstractions \
	-l none --keep-facades true --verbose --disable-opt unreachablebodies \
	--strip-security true --exclude-feature com --exclude-feature sre

DOTNET_LINKER_ARGS := -out $(LINKER_OUTPUT) -b true -c link -l none --dump-dependencies
WASM_LINKER_ARGS := $(DOTNET_LINKER_ARGS) -d $(WASM_PROFILE_PATH)
TEST_LINKER_ARGS :=  --optimizer-xml $(ROOTDIR)/Tests/corlib-nunit.xml --optimizer-report $(LINKER_OUTPUT)/martin-report.xml --optimizer-options report-profile=wasm,report-mode=actions+size+detailed $(EXTRA_OPTIMIZER_ARGS)
BLAZOR_OUT = bin/Debug/netstandard2.0/publish

TEST_CASES := \
	test-EmptyBlazor

include $(ROOTDIR)/build/tests.make

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

$(eval $(call BlazorTest,EmptyBlazor))
