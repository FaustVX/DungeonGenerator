# Common Roguelike Symbols

A reference for symbols commonly used to represent dungeon maps, creatures, items, and effects. These are conventions, not a universal standard: a game's legend should always take precedence.

The first sections focus on plain ASCII symbols for compatibility with simple terminals and fonts. The Unicode section gives optional alternatives and decorative symbols.

## Tiles and terrain

| Symbol | Common meaning |
|:---:|---|
| `#` | Wall |
| `.` | Floor or open ground |
| ` ` | Unexplored area, darkness, or empty space |
| `+` | Closed door |
| `'` | Open door or doorway |
| `<` | Stairs or passage up |
| `>` | Stairs or passage down |
| `^` | Trap or dangerous tile |
| `~` | Water, lava, or another liquid |
| `:` | Rubble, rough ground, or moss |
| `,` | Grass, shallow water, or debris |
| `;` | Tall grass or dense vegetation |
| `-` | Horizontal wall, fence, or boundary |
| `\|` | Vertical wall, fence, or boundary |
| `=` | Bridge or special floor |
| `"` | Chasm, shallow water, or another impassable feature |

## Creatures and characters

Lowercase letters commonly identify weaker or smaller creatures; uppercase letters often represent larger or more dangerous ones. The exact mapping is game-specific.

| Symbol | Common meaning |
|:---:|---|
| `@` | Player character |
| `a` | Ant or insect |
| `b` | Bat |
| `c` | Canine |
| `d` | Dragon |
| `e` | Elemental |
| `f` | Floating eye or other floating creature |
| `g` | Goblin |
| `h` | Human or humanoid |
| `k` | Kobold |
| `m` | Mouse or small mammal |
| `o` | Orc |
| `r` | Rodent, often a rat |
| `s` | Snake or spider |
| `t` | Troll |
| `w` | Worm |
| `z` | Zombie |
| `A` | Angel or powerful flying creature |
| `D` | Dragon |
| `G` | Giant |
| `O` | Ogre or large creature |
| `S` | Skeleton |
| `T` | Troll |
| `M` | Powerful or miscellaneous monster |

## Items and equipment

| Symbol | Common meaning |
|:---:|---|
| `!` | Potion |
| `?` | Scroll |
| `/` | Wand |
| `\` | Staff |
| `)` | Weapon |
| `[` | Armor |
| `=` | Ring |
| `"` | Amulet |
| `(` | Tool or miscellaneous equipment |
| `%` | Food, ration, or corpse |
| `$` | Gold or currency |
| `*` | Gem, jewel, or valuable item |
| `}` | Missile or thrown weapon |

## Objects and points of interest

| Symbol | Common meaning |
|:---:|---|
| `_` | Altar |
| `&` | Statue, shrine, or special object |
| `0` | Fountain |
| `O` | Boulder or large object |
| `B` | Bed, bedroll, or furniture |
| `C` | Chest or container |
| `^` | Trap or trigger |
| `+` | Door, gate, or portal |

## Effects and hazards

| Symbol | Common meaning |
|:---:|---|
| `*` | Sparkle, explosion, or spell impact |
| `!` | Alert, impact, or status effect |
| `?` | Unknown, unidentified, or magical effect |
| `~` | Smoke, fire, liquid, or an energy field |
| `^` | Trap, spike, or warning marker |
| `x` | Hit marker, debris, or small effect |

## Unicode alternatives

These are optional visual alternatives, not fixed roguelike conventions. Each listed symbol is a single Unicode code point in the Basic Multilingual Plane (BMP), so it fits in one C# `char`. Rendering depends on the terminal, font, and text encoding.

| Symbol | Possible use |
|:---:|---|
| `█` | Solid wall |
| `▓` | Heavy wall or dense terrain |
| `▒` | Partial wall, fog, or rough terrain |
| `░` | Dim or unexplored area |
| `·` | Floor |
| `◆` | Gem, objective, or point of interest |
| `◇` | Empty gem socket or secondary objective |
| `♠` | Creature, danger, or faction marker |
| `♥` | Health, life, or healing |
| `♦` | Treasure or valuable item |
| `♣` | Plant, nature, or faction marker |
| `☼` | Light source or sun |
| `☠` | Poison, death, or undead |
| `⚑` | Flag, marker, or objective |
| `⚔` | Combat or weapon |
| `⚒` | Tool, crafting, or work |
| `⇧` | Upward passage |
| `⇩` | Downward passage |
| `≈` | Water |
| `♨` | Heat, fire, or hot spring |
