# Special Trash and Tools Setup Guide

This guide covers setup for BigRock, Parasite trash items and the Destructor/Heater tools.

## BigRock Setup

BigRock is a large, immovable trash item that requires the Destructor tool to break down through multiple visual stages.

### Basic Setup

1. Create an empty GameObject and name it (e.g., "BigRock_01")
2. Add a **Collider** (BoxCollider, SphereCollider, or MeshCollider)
3. Add a **Rigidbody** component:
   - Set `Is Kinematic` to **true**
   - Set `Use Gravity` to **false**
4. Add the **BigRock** component
5. Set the GameObject's **Layer** to `ToolTarget`

### BigRock Component Configuration

- **Weight**: The weight value for trash tracking (default: 1)
- **Max Health**: Total health points (default: 100)
- **Persistent Id**: Unique identifier for saving removal state across sessions
  - **Must be unique within the scene**
  - Leave empty to disable persistence
  - When duplicating BigRock objects, always change this ID

### Visual Stages

BigRock displays different meshes based on remaining health:

- **Visual Full**: Active at 100% - 76% health
- **Visual 75**: Active at 75% - 51% health
- **Visual 50**: Active at 50% - 26% health
- **Visual 25**: Active at 25% - 1% health

Assign child GameObjects with MeshRenderer for each stage. When health reaches 0%, the entire BigRock is removed and destroyed.

**Placeholder Setup** (until final art is ready):
1. Create 4 child objects: "Stage_Full", "Stage_75", "Stage_50", "Stage_25"
2. Add Cube or Sphere meshes with different scales/materials to distinguish them
3. Assign each to the corresponding Visual field in BigRock component

### Example Hierarchy

```
BigRock_01 (Layer: ToolTarget)
├─ BigRock (component)
├─ BoxCollider
├─ Rigidbody (kinematic)
├─ Stage_Full (active by default)
├─ Stage_75 (inactive)
├─ Stage_50 (inactive)
└─ Stage_25 (inactive)
```

---

## Parasite Setup

Parasite is a smaller trash item that shrinks as it takes damage from the Heater tool.

### Basic Setup

1. Create a GameObject and name it (e.g., "Parasite_01")
2. Add a **Collider** (SphereCollider or CapsuleCollider recommended)
3. Add a **Rigidbody** (optional, but recommended for dispose zone detection):
   - Set `Is Kinematic` to **true**
   - Set `Use Gravity` to **false**
4. Add the **Parasite** component
5. Set the GameObject's **Layer** to `ToolTarget`

### Parasite Component Configuration

- **Weight**: The weight value for trash tracking (default: 1)
- **Max Health**: Total health points (default: 50)
- **Persistent Id**: Unique identifier for saving removal state
  - **Must be unique within the scene**
  - Leave empty to disable persistence
  - When duplicating Parasite objects, always change this ID
- **Visual Root**: Transform to scale (default: self)
  - Assign a child object if you want the root to stay fixed while the visual shrinks
  - Leave empty to scale the entire GameObject

### Visual Behavior

The Parasite smoothly scales from its initial size to zero as health decreases. At 0% health, it's removed and destroyed.

### Example Hierarchy

```
Parasite_01 (Layer: ToolTarget)
├─ Parasite (component)
├─ SphereCollider
├─ Rigidbody (kinematic, optional)
└─ VisualMesh (MeshRenderer)
```

---

## Destructor Tool

The Destructor tool fires a visible beam that damages BigRock objects.

### Component References

- **Beam Origin**: Transform for ray origin and direction (usually weapon muzzle)
- **Beam Renderer**: LineRenderer component for visual beam

### Configuration

- **Beam Length**: Maximum beam distance (default: 50m)
- **Beam Blocker Mask**: Layers that stop the beam
  - Should include environment and `ToolTarget`
  - Should **not** include player or weapon layers
- **Beam Damage Per Second**: DPS applied to BigRock (default: 25)

### Visual Enhancement (Optional)

For multi-layered beam effect:
1. Duplicate the LineRenderer 3 times
2. Assign different materials:
   - Outer: `DestructorBeamOuter.mat` (wide, faint cyan glow)
   - Body: `DestructorBeamBody.mat` (medium, bright cyan)
   - Core: `DestructorBeamCore.mat` (thin, white/bright)
3. Set widths in a ratio like 1.0 / 0.5 / 0.1
4. Update DestructorTool to control all three LineRenderers

### Behavior

- Beam stops at first collider in Beam Blocker Mask
- Only BigRock components receive damage
- Any obstacle (walls, rocks, parasites) blocks the beam

---

## Heater Tool

The Heater tool projects a cone that damages all Parasite objects inside it.

### Component References

- **Heat Zone Origin**: Transform for cone apex and forward direction (usually weapon muzzle)
- **Cone Visual**: HeaterConeVisual component (generates cone mesh)

### Configuration

- **Cone Length**: Distance from apex to base (default: 3m)
- **Cone Opening Angle**: Full cone angle in degrees (default: 45°)
- **Damage Per Second**: Base DPS applied to parasites (default: 20)
- **Distance Damage Multiplier**: AnimationCurve controlling damage falloff
  - X-axis: normalized distance from apex (0 = apex, 1 = base)
  - Y-axis: damage multiplier (1.0 = full damage, 0.5 = half damage)
  - Default: higher damage at apex, lower at base

### Visual Setup

1. Create a child GameObject under the Heater tool (e.g., "ConeVisual")
2. Add **HeaterConeVisual** component
3. Add **MeshFilter** and **MeshRenderer** (added automatically)
4. Assign material: `HeaterCone.mat`
5. The cone is generated procedurally at runtime
6. Position and rotation are driven by Heat Zone Origin

### Behavior

- Searches for parasites in a sphere (broad phase)
- Filters by cone geometry (narrow phase)
- Damages all valid parasites simultaneously each frame
- **Does not require line of sight** - damages through walls

---

## Trash Persistence

Both BigRock and Parasite support persistence across game sessions.

### How It Works

- Each item needs a unique **Persistent Id**
- When removed, the ID is saved to PlayerPrefs
- On scene load, items check if they were previously removed
- Previously removed items are destroyed immediately
- Partial damage is **not saved** - items respawn at full health

### New Game Behavior

When the player starts a new game (presses "Новая игра" in main menu):
- Trash generation counter is incremented
- All previous removal data is invalidated
- All trash items respawn fresh

### Level Design Guidelines

1. **Always assign unique IDs** when placing BigRock or Parasite in scenes
2. Use a naming convention like: `Scene_ObjectType_Number`
   - Example: `Sample_BigRock_001`, `Sample_Parasite_012`
3. When duplicating objects, **immediately change the Persistent Id**
4. Empty or duplicate IDs will show warnings in the Console

### Debugging

- Unity Editor menu: `Tools > Clear All Trash Persistence`
- This resets the generation counter (doesn't delete individual keys)
- For complete reset, use `GameProgress.ResetProgress()` in code

---

## Integration with TrashLocation

Both BigRock and Parasite inherit from TrashItem, so they:
- Are automatically discovered by TrashLocation
- Contribute to total trash weight
- Trigger TrashLocation events when removed
- Work with TrashDisposeZone (instant removal on contact)

### Scene Setup

Place BigRock and Parasite objects as children of or near TrashLocation objects as described in `trash-location-setup.md`. The system works identically to regular trash items.

---

## Testing Checklist

### BigRock
- [ ] Collider is on ToolTarget layer
- [ ] Has kinematic Rigidbody
- [ ] Persistent Id is unique (if used)
- [ ] At least one visual stage is assigned
- [ ] Destructor beam hits and damages it
- [ ] Changes visual stages as health decreases
- [ ] Removed and destroyed at 0 health
- [ ] TrashLocation updates when removed

### Parasite
- [ ] Collider is on ToolTarget layer
- [ ] Persistent Id is unique (if used)
- [ ] Heater cone detects and damages it
- [ ] Scales smoothly as health decreases
- [ ] Removed and destroyed at 0 health
- [ ] TrashLocation updates when removed

### Persistence
- [ ] Removed items stay removed after scene reload
- [ ] "Новая игра" respawns all trash
- [ ] No duplicate ID warnings in Console

---

## Common Issues

**Destructor beam doesn't hit BigRock:**
- Check BigRock Layer is in Beam Blocker Mask
- Verify BigRock has a Collider
- Check beam origin and direction in Scene view

**Heater doesn't damage Parasite:**
- Verify Parasite has a Collider (doesn't need to be in any specific layer mask)
- Check Heater cone parameters (length, angle)
- Use Scene Gizmos to visualize cone bounds

**Trash respawns immediately after removal:**
- Check Persistent Id is not empty
- Verify no duplicate IDs in scene
- Ensure GameProgress.ResetTrash() is called on new game

**Visual stages don't change (BigRock):**
- Verify Visual references are assigned in Inspector
- Check that child objects have MeshRenderer components
- Test health values in Play Mode with Debug view
