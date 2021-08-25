log_target_info = (echo "\e[34m================\n $1\n================\e[39m")

NACARA_LAYOUT_STANDARD_DIR=src/Nacara.Layout.Standard
NACARA_DIR=src/Nacara
NACARA_CORE_DIR=src/Nacara.Core
NACARA_CREATE_DIR=src/Nacara.Create

# Base of the Fable commands
NACARA_LAYOUT_STANDARD_FABLE=dotnet fable $(NACARA_LAYOUT_STANDARD_DIR)/Source --outDir $(NACARA_LAYOUT_STANDARD_DIR)/dist
NACARA_FABLE=dotnet fable $(NACARA_DIR)/Source --outDir $(NACARA_DIR)/dist
NODEMON_WATCHER=npx nodemon \
	--watch $(NACARA_DIR)/dist \
	--watch $(NACARA_LAYOUT_STANDARD_DIR)/dist \
	--watch nacara.config.json \
	--delay 150ms \
	--handle-input \
	--exec 'nacara watch'

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
	rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/dist
	rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/Source/bin
	rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/Source/obj
# Clean Nacara artifacts
	rm -rf $(NACARA_DIR)/dist
	rm -rf $(NACARA_DIR)/Source/bin
	rm -rf $(NACARA_DIR)/Source/obj
# Clean Nacara.Core artifacts
	rm -rf $(NACARA_CORE_DIR)/dist
	rm -rf $(NACARA_CORE_DIR)/Source/bin
	rm -rf $(NACARA_CORE_DIR)/Source/obj
# Clean generated documentation
	rm -rf docs_deploy

watch: clean
# Make sure that the dist directories exists
# Otherwise, nodemon cannot listen to them
	@$(call log_target_info, "Setup directories for the watcher")
	mkdir $(NACARA_LAYOUT_STANDARD_DIR)/dist
	mkdir $(NACARA_DIR)/dist
# Start the different watcher
# 1. Nacara
# 2. Nacara.Layout.Standard
# 3. Nodemon to restart nacara on changes
	@$(call log_target_info, "Watching...")
	npx concurrently -p none \
		"$(NODEMON_WATCHER)" \
		"$(NACARA_LAYOUT_STANDARD_FABLE) --watch --sourceMaps --run \"make nodemon\"" \
		"$(NACARA_FABLE) --watch --sourceMaps"

nodemon:
	$(NODEMON_WATCHER)

build: clean
	@$(call log_target_info, "Building...")
	$(NACARA_FABLE)
	$(NACARA_LAYOUT_STANDARD_FABLE)

generate-docs: build
	@$(call log_target_info, "Generating documentation...")
	npx nacara

release: build
	@$(call log_target_info, "Releasing...")
	@# Remove .fable/.gitignore files otherwise npm doesn't publish that directory
	rm -rf $(NACARA_DIR)/dist/.fable/.gitignore
	rm -rf $(NACARA_LAYOUT_STANDARD_DIR)/dist/.fable/.gitignore
	@# Publish the packages
	node ./scripts/release-npm.js $(NACARA_DIR)
	node ./scripts/release-nuget.js $(NACARA_CORE_DIR) Nacara.Core.fsproj
	node ./scripts/release-npm.js $(NACARA_LAYOUT_STANDARD_DIR)
	node ./scripts/release-npm.js $(NACARA_CREATE_DIR)

publish: release generate-docs
	@$(call log_target_info, "Publishing...")
	npx gh-pages --dist docs_deploy
