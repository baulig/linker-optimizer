thisdir = tools/linker/Tests/Martin
SUBDIRS =
include ../../../../build/rules.make

# LINKER_PROFILE_OPTIONS += --exclude-feature remoting --exclude-feature com --exclude-feature etw

LINKER_OUTPUT := output
PROFILE_PATH = $(topdir)/class/lib/$(PROFILE_DIRECTORY)
LINKER = MONO_PATH=$(topdir)/class/lib/$(BUILD_TOOLS_PROFILE) $(RUNTIME) $(RUNTIME_FLAGS) --debug $(topdir)/class/lib/$(BUILD_TOOLS_PROFILE)/monolinker.exe -c link -out $(LINKER_OUTPUT) -b true -d $(PROFILE_PATH)
LINKER_DEFAULT = $(LINKER) -l none $(LINKER_PROFILE_OPTIONS)
TEST_EXEC = MONO_PATH=$(LINKER_OUTPUT) $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot

TESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(topdir)/class/lib/$(PROFILE_DIRECTORY)/mscorlib.dll

CLEAN_FILES += *.exe *.dll *.pdb

CORLIB_SOURCES := \
	$(topdir)/class/corlib/System.Runtime.CompilerServices/MonoLinkerFeature.cs \
	$(topdir)/class/corlib/System.Runtime.CompilerServices/MonoLinkerSupport.cs

IL_HELPER_SOURCES := \
	il-support.il \
	test-helpers.il

TEST_HELPER_SOURCES := \
	AssertionException.cs \
	TestHelpers.cs

TEST_CASES := \
	test-weak-instances-1.cs \
	test-weak-instances-2.cs \
	test-weak-instances-3.cs \
	test-weak-instances-4.cs \
	test-weak-instances-5.cs \
	test-weak-instances-6.cs \
	test-features-1.cs \
	test-features-2.cs \
	test-features-3.cs \
	test-features-4.cs \
	test-features-5.cs \
	test-features-6.cs \
	test-features-7.cs \
	test-conditionals-1.cs \
	test-conditionals-2.cs \
	test-conditionals-3.cs \
	test-conditionals-4.cs \
	test-conditionals-5.cs \
	test-flow-analysis-1.cs \
	test-flow-analysis-2.cs \
	test-flow-analysis-3.cs \
	test-flow-analysis-4.cs \
	test-globalization-1.cs

BROKEN_TESTS :=

ILTEST_CASES := \
	iltest-conditionals-1.il \
	iltest-flow-analysis-1.il

TEST_HELPERS_LIBRARY := TestHelpers.dll

$(TEST_HELPERS_LIBRARY): $(TEST_HELPER_SOURCES)
	$(TESTS_COMPILER) /t:library /out:$@ $^

compile-tests: $(TEST_CASES:.cs=.exe) $(ILTEST_CASES:.il=.exe) $(BROKEN_TESTS:.cs=.exe) $(TEST_HELPERS_LIBRARY)

run: $(TEST_CASES:.cs=) $(ILTEST_CASES:.il=) build

test-%.exe: test-%.cs $(TEST_HELPERS_LIBRARY)
	$(TESTS_COMPILER) /optimize /out:$@ /r:$(TEST_HELPERS_LIBRARY) $(filter test-%.cs,$^)

iltest-%.exe: iltest-%.il $(TEST_HELPERS_LIBRARY)
	$(ILASM) /out:$@ $(filter iltest-%.il,$^)

define RunTests
test-$(1): build $(patsubst %.cs,%,$(filter test-$(1)-%.cs,$(TEST_CASES)))
endef

define RunIlTests
iltest-$(1): build $(patsubst %.il,%,$(filter iltest-$(1)-%.il,$(ILTEST_CASES)))
endef

iltests: build $(ILTEST_CASES:.il=)

$(eval $(call RunTests,weak-instances))
$(eval $(call RunTests,features))
$(eval $(call RunTests,conditionals))
$(eval $(call RunTests,flow-analysis))
$(eval $(call RunTests,globalization))

$(eval $(call RunIlTests,conditionals))
$(eval $(call RunIlTests,flow-analysis))

test-%: test-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER_DEFAULT) --dump-dependencies --martin -a $@ $(if $(wildcard $(@F).xml), -i $(@F).xml)
	@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

iltest-%: iltest-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER_DEFAULT) --dump-dependencies --martin -a $@
	@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

build::
	$(MAKE) PROFILE=build -C ../..

clean:
	@rm -rf $(LINKER_OUTPUT)
	@rm -f $(executable_CLEAN_FILES) $(CLEAN_FILES) $(tests_CLEAN_FILES) $(TEST_HELPERS_LIBRARY)

all: build compile-tests
