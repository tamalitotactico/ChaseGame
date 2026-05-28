---
name: analyze
description: Scan a Unity project's codebase and propose architecture improvements — patterns, anti-patterns, tech debt, and structural recommendations.
allowed-tools: Bash, Read, Write, Edit, Glob, Grep
argument-hint: <focus area, e.g. "combat system" or "full project scan">
metadata:
  mcpmarket-version: 1.0.0
---
Analyze the Unity project architecture: $ARGUMENTS

## Process

1. **Scan the codebase** — find all C# scripts, identify namespaces, class hierarchies, dependencies
2. **Map the architecture**:
   - Identify MonoBehaviours, ScriptableObjects, static classes, managers, singletons
   - Map dependency graph (who references whom)
   - Find event buses, service locators, DI containers
3. **Evaluate patterns**:
   - Is the right pattern used for the right job? (ECS vs MonoBehaviour, events vs polling)
   - Are there god classes or tight coupling?
   - Is serialization used correctly?
   - Are Unity lifecycle methods used efficiently?
4. **Flag issues**:
   - Anti-patterns (Update polling, string-based Find calls, public fields)
   - Tech debt (TODO comments, commented-out code, unused scripts)
   - Performance concerns (allocation in Update, expensive operations in hot paths)
5. **Recommend improvements** with concrete code examples
6. **Generate architecture diagram** (text-based) showing system relationships

## Architecture Patterns to Evaluate

- Singleton / Service Locator / DI
- Observer / Event Bus / UnityEvent
- State Machine / Behavior Tree
- Command Pattern for undo/redo
- Object Pooling for frequently spawned objects
- ScriptableObject-based configuration
- Assembly Definition structure
