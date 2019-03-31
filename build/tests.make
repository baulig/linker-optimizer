ifdef TESTNAME
TESTNAME_ARG = -test=$(TESTNAME)
endif

standalone-all:: compile-tests

TEST_HELPERS_LIBRARY = ../TestHelpers/TestHelpers.dll

$(TEST_HELPERS_LIBRARY):
	$(MAKE) -C ../TestHelpers

CLEAN_DIRECTORIES += $(LINKER_OUTPUT)

compile-tests:: $(TEST_CASES:.cs=.exe) $(ILTEST_CASES:.il=.exe) $(AOTTEST_CASES:.cs=.exe) $(BROKEN_TESTS:.cs=.exe) $(TEST_HELPERS_LIBRARY)

run: $(TEST_CASES:.cs=) $(AOTTEST_CASES:.cs=) $(ILTEST_CASES:.il=) build

test-%.exe: test-%.cs $(TEST_HELPERS_LIBRARY)
	$(TESTS_COMPILER) /optimize /out:$@ /r:$(TEST_HELPERS_LIBRARY) $(filter test-%.cs,$^)

aottest-%.exe: aottest-%.cs $(TEST_HELPERS_LIBRARY)
	$(AOTTESTS_COMPILER) /optimize /out:$@  $(filter aottest-%.cs,$^)

iltest-%.exe: iltest-%.il $(TEST_HELPERS_LIBRARY)
	$(ILASM) /out:$@ $(filter iltest-%.il,$^)

define RunTests
test-$(1): build $(patsubst %.cs,%,$(filter test-$(1)-%.cs,$(TEST_CASES)))
endef

define RunIlTests
iltest-$(1): build $(patsubst %.il,%,$(filter iltest-$(1)-%.il,$(ILTEST_CASES)))
endef

test-%: test-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin $@ $(LINKER_ARGS_DEFAULT) --dump-dependencies
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

aottest-%: aottest-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin $@ $(LINKER_ARGS_AOT) --dump-dependencies
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(AOTPROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(AOTPROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(AOTPROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

iltest-%: iltest-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin $@ $(LINKER_ARGS_DEFAULT) --dump-dependencies
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)
