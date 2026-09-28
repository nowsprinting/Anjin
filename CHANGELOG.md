# Changelog

This repository is forked from [DeNA/Anjin](https://github.com/DeNA/Anjin) v1.9.0.

- For changes up to v1.9.0, see the [upstream GitHub Releases](https://github.com/DeNA/Anjin/releases).
- This file lists only the changes made in this fork since then.
- The package version is not incremented in this fork; it remains 1.9.0.

## Changes since forked from DeNA/Anjin v1.9.0

### 🐛 Bug Fixes

- Fix compile errors on Unity 6000.6 (EntityId / AssetCreationEndAction) by @AtsuAtsu0120 in [#9](https://github.com/nowsprinting/Anjin/pull/9)

### 🧰 Maintenance

- Change UGUIPlaybackAgent to now optional (tradeoff with Unity 6000.2.6f1 and later) by @nowsprinting in [#1](https://github.com/nowsprinting/Anjin/pull/1)
- Mod to run tests on: PR and push to master by @nowsprinting in [#2](https://github.com/nowsprinting/Anjin/pull/2)
- Upgrade dependent test-helper.monkey package to test-helper.ui package by @nowsprinting in [#4](https://github.com/nowsprinting/Anjin/pull/4)
- Upgrade Unity versions used to run tests on CI by @nowsprinting in [#7](https://github.com/nowsprinting/Anjin/pull/7)
- Upgrade dependent test-helper.ui package to v1.2.2 by @nowsprinting in [#8](https://github.com/nowsprinting/Anjin/pull/8)
- Upgrade Unity versions used to run tests on CI (Unity 6.6) by @nowsprinting in [#10](https://github.com/nowsprinting/Anjin/pull/10)
