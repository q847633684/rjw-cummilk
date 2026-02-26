# AGENTS.md

## Cursor Cloud specific instructions

### Project overview
This is a RimWorld mod ("EqualMilking" / 精液和母乳) written in C#/.NET. It has 4 `.csproj` projects under `Source/` (no `.sln` file), targeting .NET Framework 4.8/4.8.1. The pre-built mod DLL ships in `Assemblies/EqualMilking.dll`.

### Build system
- Build tool: `dotnet build` (MSBuild SDK-style projects)
- Configuration name: `1.6` (not `Debug`/`Release`)
- Main project: `Source/EqualMilking/EqualMilking.csproj`
- Integration sub-projects: `Source/RJW/`, `Source/PipeSystem/`, `Source/VME_Patch/`
- NuGet package: `Krafs.Publicizer` (for publicizing private game APIs)
- `Directory.Build.props` defines shared settings; `Directory.Build.targets` adds `Microsoft.NETFramework.ReferenceAssemblies` for Linux targeting and redirects Windows-specific DLL paths to local stubs.

### External dependencies (game DLLs)
Full compilation requires proprietary RimWorld game DLLs (Assembly-CSharp, Unity, Harmony) and mod DLLs (RJW, PipeSystem, Building_Milk) which are not included in the repo. On a developer's Windows machine, these are found at Steam install paths. On this Linux cloud VM:
- Stub DLLs are generated from projects in `_stubs/` and placed at:
  - `RimWorld_Data/1.6/` (Assembly-CSharp, UnityEngine, Unity modules)
  - `Libs/Workshop/2009463077/Current/Assemblies/` (Harmony)
  - `Libs/Mods/rjw/1.6/Assemblies/` (RJW)
  - `Libs/Workshop/2023507013/1.6/Assemblies/` (PipeSystem)
  - `Libs/Building_Milk.dll`
- These stubs define type signatures only; builds will show errors for missing members. The stubs reduce errors from thousands to ~30 for type/access-modifier issues.

### Building
```bash
# Restore NuGet packages
dotnet restore Source/EqualMilking/EqualMilking.csproj

# Build (will partially fail without real game DLLs)
dotnet build Source/EqualMilking/EqualMilking.csproj -c 1.6

# Rebuild stubs (if needed)
cd _stubs/AssemblyCSharp && dotnet build -c Release
cd _stubs/HarmonyStub && dotnet build -c Release
cd _stubs/RJWStub && dotnet build -c Release
cd _stubs/PipeSystemStub && dotnet build -c Release
```

### Key caveats
- No `.sln` file exists; build individual `.csproj` files directly
- No automated tests exist in this repo
- No lint configuration (e.g., `.editorconfig`, analyzers) is set up
- The mod cannot be "run" standalone; it must be loaded by the RimWorld game engine
- `_stubs/` has its own `Directory.Build.props` to block inheritance from the workspace root
