thisdir = tools/linker/Tests/Martin
SUBDIRS =
include ../../../../build/rules.make

PROGRAM = martin-test.exe

LINKER_PROFILE_OPTIONS += --exclude-feature remoting --exclude-feature com --exclude-feature etw

LINKER_OUTPUT := output
PROFILE_PATH = $(topdir)/class/lib/$(PROFILE_DIRECTORY)
LINKER = MONO_PATH=$(topdir)/class/lib/$(BUILD_TOOLS_PROFILE) $(RUNTIME) $(RUNTIME_FLAGS) $(topdir)/class/lib/$(BUILD_TOOLS_PROFILE)/monolinker.exe -c link -out $(LINKER_OUTPUT) -b true -d $(PROFILE_PATH)
LINKER_DEFAULT = $(LINKER) -l none $(LINKER_PROFILE_OPTIONS)

martin-test: martin-test.cs MonoLinkerSupport.cs LinkerTest.cs $(PROFILE_PATH)/martin-test.exe
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	@echo MARTIN TEST
#	csc /optimize /out:Tests/Martin/martin-test.exe Tests/Martin/martin-test.cs Tests/Martin/MonoLinkerSupport.cs Tests/Martin/LinkerTest.cs
	$(LINKER_DEFAULT) --verbose --dump-dependencies --martin -a $(PROFILE_PATH)/martin-test.exe
	gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	ls -lR $(LINKER_OUTPUT)
	#(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./martin-test.exe)
	#@rm -rf $(LINKER_OUTPUT)


include ../../../../build/executable.make

clean-local:
	@rm -rf $(LINKER_OUTPUT)
	@rm -f $(executable_CLEAN_FILES) $(CLEAN_FILES) $(tests_CLEAN_FILES)

