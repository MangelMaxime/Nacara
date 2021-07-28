log_target_info = (echo "\e[34m================\n $1\n================\e[39m")

NACARA_LAYOUT_STANDARD_DIR=src/Nacara.Layout.Standard
NACARA_DIR=src/Nacara
NACARA_CORE_DIR=src/Nacara.Core

# Base of the Fable commands
NACARA_LAYOUT_STANDARD_FABLE=dotnet fable $(NACARA_LAYOUT_STANDARD_DIR)/Source --outDir $(NACARA_LAYOUT_STANDARD_DIR)/dist
NACARA_FABLE=dotnet fable $(NACARA_DIR)/Source --outDir $(NACARA_DIR)/dist
NODEMON_WATCHER=npx nodemon \
	--watch src/Nacara/dist \
	--watch src/Nacara.Layout.Standard/dist \
	--watch nacara.config.json \
	--delay 150ms \
	--exec \"nacara --watch\"

setup-dev:
	@$(call log_target_info, "Setting up the npm link for local development")
	npm install
	dotnet tool restore
	cd $(NACARA_DIR) && npm install
	cd $(NACARA_LAYOUT_STANDARD_DIR) && npm install
	cd $(NACARA_DIR) && npm link
	cd $(NACARA_LAYOUT_STANDARD_DIR) && npm link
	npm link nacara nacara-layout-standard

unsetup-dev:
	@$(call log_target_info, "Unsetting the npm link for local development")
	npm unlink nacara nacara-layout-standard
	cd $(NACARA_DIR) && npm -g unlink
	cd $(NACARA_LAYOUT_STANDARD_DIR) && npm -g unlink

clean:
	@$(call log_target_info, "Cleaning...")
# Clean Nacara.Layout.Standard artifacts
	npx shx rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/dist
	npx shx rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/Source/bin
	npx shx rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/Source/obj
# Clean Nacara artifacts
	npx shx rm -rf $(NACARA_DIR)/dist
	npx shx rm -rf $(NACARA_DIR)/Source/bin
	npx shx rm -rf $(NACARA_DIR)/Source/obj
# Clean Nacara.Core artifacts
	npx shx rm -rf $(NACARA_CORE_DIR)/dist
	npx shx rm -rf $(NACARA_CORE_DIR)/Source/bin
	npx shx rm -rf $(NACARA_CORE_DIR)/Source/obj
# Clean generated documentation
	npx shx rm -rf temp

watch: clean
# Make sure that the dist directories exists
# Otherwise, nodemon cannot listen to them
	@$(call log_target_info, "Setup directories for the watcher")
	npx shx rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/dist
	npx shx rm -rf $(NACARA_DIR)/dist
# Start the different watcher
# 1. Nacara
# 2. Nacara.Layout.Standard
# 3. Nodemon to restart nacara on changes
	@$(call log_target_info, "Watching...")
	npx concurrently -p none \
		"$(NACARA_LAYOUT_STANDARD_FABLE) --watch --sourceMaps" \
		"$(NACARA_FABLE) --watch --sourceMaps" \
		"$(NODEMON_WATCHER)"

build: clean
	@$(call log_target_info, "Building...")
	$(NACARA_FABLE)
	$(NACARA_LAYOUT_STANDARD_FABLE)

generate-docs: build
	@$(call log_target_info, "Generating documentation...")
	npx nacara

release: build
	@$(call log_target_info, "Releasing...")
	node ./scripts/release-npm.js $(NACARA_DIR)
	node ./scripts/release-nuget.js $(NACARA_CORE_DIR) Nacara.Core.fsproj
	node ./scripts/release-npm.js $(NACARA_LAYOUT_STANDARD_DIR)
