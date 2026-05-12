# Modular Combat System Prototype ⚔️

![Project Status](https://img.shields.io/badge/Status-Work--In--Progress-orange)
![Unity Version](https://img.shields.io/badge/Unity-2022.3%20LTS-blue)

A high-fidelity, modular third-person combat system built in Unity, featuring a robust architecture for weapon swapping, combo systems, and AI-driven enemy encounters.

## 🚧 Work In Progress
This project is currently under active development. Core combat mechanics are implemented, and the system is being refined for better modularity and performance.

## 🚀 Key Features
- **Modular Weapon System**: Easily swap between different weapon types (Melee, Magic, Ranged) using ScriptableObjects.
- **Dynamic Combo System**: Multi-hit combo strings with timed input windows and damage multipliers.
- **Advanced AI**: Stone Golem enemy with patrol, aggro, and slam attack behaviors using Unity NavMesh.
- **Visual Excellence**: Built with Unity's Universal Render Pipeline (URP) for high-quality visuals and optimized performance.
- **VFX Integration**: Weapon trails, hit sparks, and impact effects driven by Unity's Visual Effect Graph.
- **Cinemachine Support**: Fluid camera follow and lock-on mechanics for a professional feel.

## 🏗️ System Architecture
The system is designed with a "Module-first" approach:
- **`CombatManager`**: Central hub for managing game state and global combat events.
- **`WeaponData`**: ScriptableObject-based weapon configuration (stats, animations, prefabs).
- **`WeaponModule`**: Handles the physical instantiation and hitbox logic of weapons.
- **`HealthSystem`**: A decoupled component for managing HP, damage, and death events for both Player and AI.

## 📁 Repository Structure
- **`/My project`**: The main Unity project folder.
- **`/Assets`**: (Unity-standard) Scripts, Models, Prefabs, and VFX.
- **`/Documentation`**: Architecture diagrams and build guides.

## 🛠️ Setup Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/nxtArnav-sharma/MCS-Prototype.git
   ```
2. Open the project folder in **Unity 2022.3 LTS** or newer.
3. Ensure the following packages are installed via Package Manager:
   - Cinemachine
   - AI Navigation
   - Input System
   - Visual Effect Graph
4. Open the `MainScene` in `Assets/Scenes/`.
5. Press **Play** to test the combat system!

## 🎮 Controls
- **WASD**: Movement
- **Left Click**: Attack / Continue Combo
- **E**: Special Ability
- **Space / Shift**: Dodge Roll
- **Q**: Swap Weapon

---
*Developed as a portfolio-ready prototype for modular game systems.*
