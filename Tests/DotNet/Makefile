thisdir = tools/linker/Martin/Tests/DotNet

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

DOTNET = dotnet
DOTNET_RID = osx.10.14-x64

include $(ROOTDIR)/build/rules.make

TEST_LINKER_ARGS := --optimizer-report $(LINKER_OUTPUT)/martin-report.xml --optimizer-options report-profile=$(PROFILE)

TEST_CASES := \
	test-dotnet-1.cs

include $(ROOTDIR)/build/tests.make

define DotNetTest
dotnet-$(1): $(1)/Program.cs $(1)/$(1).csproj standalone-build
	@echo Running test $(1)
	(cd $(1) && dotnet build)
	(cd $(1) && dotnet publish -r $(DOTNET_RID) --self-contained true)
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(if $(V),,@) $(LINKER) --optimizer $(1)/bin/Debug/netcoreapp3.0/$(DOTNET_RID)/publish/$(1).dll --dump-dependencies
endef

$(eval $(call DotNetTest,EmptyTest))

