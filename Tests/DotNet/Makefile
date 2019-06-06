thisdir = tools/linker/Martin/Tests/DotNet

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

DOTNET = dotnet
DOTNET_RID = osx.10.14-x64

include $(ROOTDIR)/build/rules.make

DOTNET_LINKER_ARGS := -out $(LINKER_OUTPUT) -b true -c link -l none --dump-dependencies
TEST_LINKER_ARGS := --optimizer-report $(LINKER_OUTPUT)/martin-report.xml --optimizer-options report-profile=$(PROFILE)
PUBLISH_OUT = bin/Debug/netcoreapp3.0/$(DOTNET_RID)/publish

TEST_CASES := \
	test-dotnet-1.cs

include $(ROOTDIR)/build/tests.make

define DotNetTest
dotnet-$(1): $(1)/Program.cs $(1)/$(1).csproj standalone-build
	@echo Running test $(1)
	@rm -rf $(1)/bin $(1)/obj
	(cd $(1) && dotnet build)
	(cd $(1) && dotnet publish -r $(DOTNET_RID) --self-contained true)
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(if $(V),,@) $(LINKER) --optimizer $(1)/$(PUBLISH_OUT)/$(1).dll $(TEST_LINKER_ARGS) $(DOTNET_LINKER_ARGS)
endef

$(eval $(call DotNetTest,EmptyTest))

