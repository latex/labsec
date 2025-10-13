# Simple Makefile for building and installing omama-cli (Linux)

# Configurables
PROJECT        ?= omama-cli/omama-cli.csproj
CONFIG         ?= Release
RID            ?= linux-x64
SELF_CONTAINED ?= false
SINGLE_FILE    ?= true
TRIM           ?= false
DOTNET_VER     ?= net9.0

# Compute publish properties based on flags
PUBLISH_PROPS  :=
ifeq ($(SINGLE_FILE),true)
PUBLISH_PROPS  += -p:PublishSingleFile=true
ifeq ($(SELF_CONTAINED),true)
PUBLISH_PROPS  += -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
endif
else
PUBLISH_PROPS  += -p:PublishSingleFile=false
endif
ifneq ($(TRIM),false)
PUBLISH_PROPS  += -p:TrimUnusedDependencies=$(TRIM)
endif

# Install prefix (default user-local). Use `sudo make install PREFIX=/usr/local` for system-wide.
PREFIX        ?= /usr
BINDIR        := $(PREFIX)/bin

APP           := omama-cli
PUBLISH_DIR    := omama-cli/bin/$(CONFIG)/$(DOTNET_VER)/$(RID)/publish
LIBDIR         := $(PREFIX)/lib/omama-cli

.PHONY: all build test publish install install-user install-system uninstall run clean

all: build

build:
	dotnet build $(PROJECT) -c $(CONFIG)

test:
	dotnet test tests/OmamaCli.Tests/OmamaCli.Tests.csproj -c $(CONFIG) -v minimal

publish:
	dotnet publish $(PROJECT) -c $(CONFIG) -r $(RID) --self-contained $(SELF_CONTAINED) $(PUBLISH_PROPS)

install: install-user

install-user: publish
	@mkdir -p "$(BINDIR)"
	@if [ "$(SINGLE_FILE)" = "true" ] && [ -x "$(PUBLISH_DIR)/$(APP)" ]; then \
		install -m 0755 "$(PUBLISH_DIR)/$(APP)" "$(BINDIR)/$(APP)"; \
		echo "Installed single-file $(APP) to $(BINDIR)/$(APP)"; \
	else \
		mkdir -p "$(LIBDIR)"; \
		if command -v rsync >/dev/null 2>&1; then \
			rsync -a --delete "$(PUBLISH_DIR)/" "$(LIBDIR)/"; \
		else \
			rm -rf "$(LIBDIR)"/*; \
			cp -r "$(PUBLISH_DIR)"/* "$(LIBDIR)/"; \
		fi; \
		echo "Installed folder contents to $(LIBDIR)"; \
		echo '#!/usr/bin/env bash' > "$(BINDIR)/$(APP)"; \
		echo 'set -euo pipefail' >> "$(BINDIR)/$(APP)"; \
		echo 'APPDIR="$(LIBDIR)"' >> "$(BINDIR)/$(APP)"; \
		echo 'if [ -x "$$APPDIR/$(APP)" ]; then' >> "$(BINDIR)/$(APP)"; \
		echo '  exec "$$APPDIR/$(APP)" "$$@"' >> "$(BINDIR)/$(APP)"; \
		echo 'else' >> "$(BINDIR)/$(APP)"; \
		echo '  exec dotnet "$$APPDIR/$(APP).dll" "$$@"' >> "$(BINDIR)/$(APP)"; \
		echo 'fi' >> "$(BINDIR)/$(APP)"; \
		chmod 0755 "$(BINDIR)/$(APP)"; \
		echo "Installed wrapper to $(BINDIR)/$(APP)"; \
	fi
	@echo "Tip: to use system cache/run dirs, set OMAMA_CACHE_DIR=/var/lib/$(APP)/cache and OMAMA_RUN_DIR=/var/run/$(APP) or use 'make install-system' with sudo."

install-system: publish
	@if [ "$(PREFIX)" != "/usr" ]; then echo "Warning: install-system expects PREFIX=/usr (current '$(PREFIX)')"; fi
	@mkdir -p "/usr/bin" || true
	@if [ "$(SINGLE_FILE)" = "true" ] && [ -x "$(PUBLISH_DIR)/$(APP)" ]; then \
		install -m 0755 "$(PUBLISH_DIR)/$(APP)" "/usr/bin/$(APP)"; \
		echo "Installed single-file $(APP) to /usr/bin/$(APP)"; \
	else \
		mkdir -p "/usr/lib/$(APP)"; \
		if command -v rsync >/dev/null 2>&1; then \
			rsync -a --delete "$(PUBLISH_DIR)/" "/usr/lib/$(APP)/"; \
		else \
			rm -rf "/usr/lib/$(APP)"/*; \
			cp -r "$(PUBLISH_DIR)"/* "/usr/lib/$(APP)/"; \
		fi; \
		echo "Installed folder contents to /usr/lib/$(APP)"; \
		echo '#!/usr/bin/env bash' > "/usr/bin/$(APP)"; \
		echo 'set -euo pipefail' >> "/usr/bin/$(APP)"; \
		echo 'APPDIR="/usr/lib/$(APP)"' >> "/usr/bin/$(APP)"; \
		echo 'if [ -x "$$APPDIR/$(APP)" ]; then' >> "/usr/bin/$(APP)"; \
		echo '  exec "$$APPDIR/$(APP)" "$$@"' >> "/usr/bin/$(APP)"; \
		echo 'else' >> "/usr/bin/$(APP)"; \
		echo '  exec dotnet "$$APPDIR/$(APP).dll" "$$@"' >> "/usr/bin/$(APP)"; \
		echo 'fi' >> "/usr/bin/$(APP)"; \
		chmod 0755 "/usr/bin/$(APP)"; \
		echo "Installed wrapper to /usr/bin/$(APP)"; \
	fi
	@mkdir -p "/var/lib/$(APP)/cache" "/var/run/$(APP)" && echo "Ensured /var/lib/$(APP)/cache and /var/run/$(APP)"

uninstall:
	@rm -f "$(BINDIR)/$(APP)" && echo "Removed $(BINDIR)/$(APP)" || true

run:
	dotnet run --project $(PROJECT) --

clean:
	dotnet clean $(PROJECT) -c $(CONFIG)
	rm -rf "omama-cli/bin" "omama-cli/obj" 2>/dev/null || true
