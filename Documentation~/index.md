# MPB Generator Documentation

MPB Generator is a Unity Editor tool for generating, editing and applying Material Property Blocks from existing Materials and Shader Graphs.

## Documentation

Available documentation files:

- [Documentation française](MPB_Generator_Documentation_FR.pdf)
- [English documentation](MPB_Generator_Documentation_Eng.pdf)

## Quick Start

1. Select a GameObject with a Renderer.
2. Add `MPBGenerator` for a single material slot, or `MPBGeneratorList` for multiple material slots.
3. Select the material slot.
4. Click `Refresh/Generate From Material`.
5. Edit the generated properties.
6. Use `Apply MPB` if needed.
7. Use `Clear MPB` to remove the overrides.

## Package contents

- `Runtime/` — runtime components and serialized data.
- `Editor/` — custom inspectors and editor-only utilities.
- `Documentation~/` — documentation files.
- `Samples~/` — importable samples.

## Recommended Unity version

Unity 2021.3 LTS or newer.