thisdir = tools/linker/Tests/Martin
SUBDIRS =
include ../../../../build/rules.make

PROGRAM = martin-test.exe

# LINKER_PROFILE_OPTIONS += --exclude-feature remoting --exclude-feature com --exclude-feature etw

LINKER_OUTPUT := output
PROFILE_PATH = $(topdir)/class/lib/$(PROFILE_DIRECTORY)
LINKER = MONO_PATH=$(topdir)/class/lib/$(BUILD_TOOLS_PROFILE) $(RUNTIME) $(RUNTIME_FLAGS) $(topdir)/class/lib/$(BUILD_TOOLS_PROFILE)/monolinker.exe -c link -out $(LINKER_OUTPUT) -b true -d $(PROFILE_PATH)
LINKER_DEFAULT = $(LINKER) -l none $(LINKER_PROFILE_OPTIONS)
TEST_EXEC = MONO_PATH=$(LINKER_OUTPUT) $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot

TESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(topdir)/class/lib/$(PROFILE_DIRECTORY)/mscorlib.dll

CORLIB_SOURCES = $(topdir)/class/corlib/System.Runtime.CompilerServices/MonoLinkerSupport.cs

CLEAN_FILES += *.exe *.dll *.pdb

TEST_CASES := \
	LinkerTest.cs

compile-tests: $(TEST_CASES)

test-%.exe: test-%.cs $(CORLIB_SOURCES)
	$(TESTS_COMPILER) /optimize /out:$@ $^

test-%: test-%.exe
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER_DEFAULT) --dump-dependencies --martin -a $@
	gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

martin-test: martin-test.cs $(CORLIB_SOURCES) LinkerTest.cs $(PROFILE_PATH)/martin-test.exe
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	@echo MARTIN TEST
#	csc /optimize /out:Tests/Martin/martin-test.exe Tests/Martin/martin-test.cs Tests/Martin/MonoLinkerSupport.cs Tests/Martin/LinkerTest.cs
	$(LINKER_DEFAULT) --dump-dependencies --martin -a $(PROFILE_PATH)/martin-test.exe
	gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	ls -lR $(LINKER_OUTPUT)
	#(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./martin-test.exe)
	#@rm -rf $(LINKER_OUTPUT)

build::
	$(MAKE) PROFILE=build -C ../..

clean:
	@rm -rf $(LINKER_OUTPUT)
	@rm -f $(executable_CLEAN_FILES) $(CLEAN_FILES) $(tests_CLEAN_FILES)

include ../../../../build/executable.make

