thisdir = tools/linker/Martin/Tests/Regression

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

include $(ROOTDIR)/build/rules.make

TEST_CASES := \
	test-conditionals-1.cs \
	test-conditionals-2.cs \
	test-conditionals-3.cs \
	test-conditionals-4.cs \
	test-conditionals-5.cs \
	test-conditionals-6.cs

include $(ROOTDIR)/build/tests.make

$(eval $(call RunTests,weak-instances))
$(eval $(call RunTests,features))
$(eval $(call RunTests,conditionals))
$(eval $(call RunTests,flow-analysis))
$(eval $(call RunTests,globalization))
$(eval $(call RunTests,size-check))

$(eval $(call RunIlTests,conditionals))
$(eval $(call RunIlTests,flow-analysis))

