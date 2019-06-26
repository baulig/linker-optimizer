thisdir = tools/linker/Martin/Tests/Blazor

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

DOTNET = dotnet
DOTNET_RID = osx.10.14-x64

include $(ROOTDIR)/build/rules.make

DOTNET_LINKER_ARGS := -out $(LINKER_OUTPUT) -b true -c link -l none --dump-dependencies
TEST_LINKER_ARGS := --optimizer-report $(LINKER_OUTPUT)/martin-report.xml --optimizer-options report-profile=dotnet,report-mode=actions+size+detailed
BLAZOR_OUT = bin/Debug/netstandard2.0/publish

TEST_CASES := \
	test-dotnet-1.cs

include $(ROOTDIR)/build/tests.make

define BlazorTest
test-$(1): $(1)/Program.cs $(1)/$(1).csproj standalone-build
	@echo Running test $(1)
	@rm -rf $(1)/bin $(1)/obj
	(cd $(1) && dotnet build)
	(cd $(1) && dotnet publish)
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(if $(V),,@) $(LINKER) --optimizer $(1)/$(BLAZOR_OUT)/$(1)/dist/_framework/_bin/$(1).dll $(TEST_LINKER_ARGS) $(DOTNET_LINKER_ARGS)
endef

$(eval $(call BlazorTest,EmptyBlazor))
