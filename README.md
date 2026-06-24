# MPB Generator

MPB Generator is a Unity Editor tool that generates editable Material Property Block controls from existing Materials and Shader Graphs.

It allows you to modify shader properties per Renderer or per material slot without duplicating Material assets.

## Features

- Generate Material Property Block values from a Material
- Edit generated values directly in the Inspector
- Single material slot workflow with `MPBGenerator`
- Multi material slot workflow with `MPBGeneratorList`
- Material slot dropdown based on `renderer.sharedMaterials`
- Shader Graph category parsing
- Automatic Shader Graph asset detection
- Support for Color, Vector, Float, Range, Texture and Int properties
- Toggle detection for float-based boolean shader properties
- Blood Splatter example sample

## Requirements

Recommended:

- Unity 2021.3 LTS or newer
- Shader Graph for category parsing
- URP if you want to use the included Blood Splatter sample

The core MPB system can work with non Shader Graph shaders, but properties will be grouped under `Other` if no Shader Graph category data is available.

## Installation

### Package Manager

Add the package from a Git URL or place the package folder inside your project's `Packages` directory.

### Manual Installation

Copy the package folder into your Unity project.

Make sure the structure stays like this:

```text
Runtime/
Editor/
Documentation~/
Samples~/
Quick Start — Single Slot
Add MPBGenerator to a GameObject with a Renderer.
Select a Material Slot from the dropdown.
Click Refresh/Generate From Material.
Edit the generated values.
Click Apply MPB if needed.
Quick Start — Multi Slot
Add MPBGeneratorList to a GameObject with a Renderer.
Click Add MPB Slot.
Select the material slot you want to control.
Click Generate This Slot.
Edit the generated values.
Click Apply This Slot or Apply All.
Documentation

Full technical documentation is available in:

Documentation~/MPB_Generator_Documentation_FR.docx
Notes

Shader Graph categories are parsed from the .shadergraph source file. This is an editor-only feature and may require adjustments if Unity changes the internal Shader Graph file format.

License

This project is released under the MIT License.


---

## `CHANGELOG.md`

```md
# Changelog

All notable changes to this package will be documented in this file.

## [1.0.0] - 2026-06-24

### Added

- Added `MPBGenerator` single-slot workflow.
- Added `MPBGeneratorList` multi-slot workflow.
- Added `ShaderPropertyData` runtime data model.
- Added `MPBSlot` runtime data model.
- Added custom Inspector for `MPBGenerator`.
- Added custom Inspector for `MPBGeneratorList`.
- Added material slot dropdown based on `renderer.sharedMaterials`.
- Added Shader Graph category parser.
- Added automatic Shader Graph asset detection.
- Added support for Color, Vector, Float, Range, Texture and Int shader properties.
- Added toggle detection for float-based boolean properties.
- Added per-material-slot MPB application.
- Added Blood Splatter sample.
- Added technical documentation.