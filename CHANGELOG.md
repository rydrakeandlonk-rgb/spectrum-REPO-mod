# Changelog

## 0.3.1
- Package the BepInEx plugin DLL as a standalone archive-root file so Vortex can route it to BepInEx/plugins.
- Stop shipping a BepInEx/plugins directory tree that can cause Vortex to manage the shared plugins directory as a deployment target.
- Add package checks that reject a BepInEx directory in the install archive.
- Keep the loader-only plugin behavior unchanged.

## 0.3.0
- Replace the previous startup implementation with a minimal BepInEx loader-validation plugin.
- Remove game-specific and networking code from the Stage 1 compiled assembly.
- Compile only the minimal plugin entry point; legacy implementation files are retained but excluded from this build.
- Keep the package limited to the Spectrum plugin and standard install metadata.

## 0.2.0
- Reworked valuable discovery and spawning around vanilla valuable definitions.
- Removed REPOLib as an installation dependency.
