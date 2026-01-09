# Multiplayer Implementation Guide

## Overview
This guide explains the 4-player system implementation (1 human + 3 AI players) where each player has their own Model, Finance, and PlayerItems.

## What Was Implemented

### 1. **Player Class** (`Assets/_Assets/Scripts/Player/Player.cs`)
   - Encapsulates all player-specific data:
     - `PlayerController` - handles movement
     - `PlayerFinance` - manages income, expenses, and cash
     - `PlayerModel` - visual representation
     - `OwnedPlayerItems` - list of properties/businesses owned
     - `AIController` - AI decision-making (for AI players only)
   - Each player has a unique ID, name, and AI status

### 2. **PlayerManager** (`Assets/_Assets/Scripts/Managers/PlayerManager.cs`)
   - Manages all 4 players
   - Handles turn-based gameplay
   - Tracks current active player
   - Provides methods to switch between players
   - Events for player turn changes

### 3. **AIController** (`Assets/_Assets/Scripts/Player/AIController.cs`)
   - Handles AI decision-making
   - Makes purchase decisions based on:
     - Available cash
     - Purchase probability settings
     - Income potential
   - Auto-rolls dice for AI players

### 4. **Updated GameManager**
   - Now works with `PlayerManager` instead of single player
   - Automatically handles AI turns
   - Switches to next player after each turn completes
   - Maintains backward compatibility with legacy single-player code

### 5. **Updated UI Systems**
   - **RealEstateUI**: Now uses current active player's finance
   - **BusinessUI**: Now uses current active player's finance
   - Both systems add PlayerItems to the current player's owned items list

## Setup Instructions

### Step 1: Create Player Prefab
1. Create a new GameObject in your scene
2. Add the following components:
   - `Player` script
   - `PlayerController` script
   - `PlayerFinance` script
3. Save as a prefab: `Assets/_Assets/Prefabs/Player.prefab`

### Step 2: Setup Player Prefab
1. In your Player prefab, make sure the player model is already included as a child GameObject
2. The model should be part of the prefab structure (not a separate prefab)
3. The Player script will automatically find the model if it has a MeshRenderer/SkinnedMeshRenderer or contains "Model" in its name

### Step 3: Setup PlayerManager in Scene
1. Create an empty GameObject named "PlayerManager"
2. Add the `PlayerManager` component
3. In the Inspector, configure:
   - **Player Prefab**: Assign the Player prefab from Step 1 (model should be inside this prefab)
   - **Player Spawn Points**: Create 4 empty GameObjects as spawn points for each player (you can have up to 4)
   - **Number Of Players**: Set to 1-4 (default: 4)
     - 1 = 1 Human player only
     - 2 = 1 Human + 1 AI
     - 3 = 1 Human + 2 AI
     - 4 = 1 Human + 3 AI

### Step 3b (Optional): Setup Player Count Selection UI
1. Create a UI panel for player count selection (before game starts)
2. Add the `PlayerCountSelector` component to the panel
3. Create buttons or a dropdown for selecting 1-4 players
4. Assign the buttons/dropdown to the component
5. Assign PlayerManager reference
6. Add a "Start Game" button that calls `StartGame()` method

### Step 4: Update GameManager
1. Find your GameManager in the scene
2. In the Inspector, assign:
   - **Player Manager Reference**: Assign the PlayerManager from Step 3
   - (Legacy player references can remain for backward compatibility)

### Step 5: Update UI References
1. Find `RealEstateUI` in your scene
2. In Inspector, assign:
   - **Player Manager**: Assign the PlayerManager
3. Repeat for `BusinessUI`

### Step 6: Configure Player Spawn Points
1. Create 4 empty GameObjects as children of your game board
2. Position them at the starting position (Path01_Start or similar)
3. Name them: "PlayerSpawnPoint_0", "PlayerSpawnPoint_1", etc.
4. Assign them to PlayerManager's "Player Spawn Points" array

## Setting Number of Players

### Method 1: Inspector (Before Play)
- Set **Number Of Players** in PlayerManager Inspector (1-4)
- The game will use this value when it starts

### Method 2: Code (Before Start)
```csharp
PlayerManager playerManager = FindObjectOfType<PlayerManager>();
playerManager.SetNumberOfPlayers(2); // Set to 2 players (1 human + 1 AI)
```

### Method 3: UI Selection (Recommended)
- Use the `PlayerCountSelector` component
- Create UI buttons or dropdown for player selection
- Players can choose 1-4 players before starting
- Call `StartGame()` when ready

## How It Works

### Turn Flow
1. **Human Player Turn**:
   - Player clicks/taps to roll dice
   - Player moves based on dice result
   - If property/business card appears, player makes decision
   - Turn ends, switches to next player

2. **AI Player Turn**:
   - AI automatically rolls dice (with slight delay for realism)
   - AI moves based on dice result
   - If property/business card appears, AI makes decision based on:
     - Available cash
     - Purchase probability (60% default)
     - Income potential
   - Turn ends, switches to next player

### Player Data Isolation
- Each player maintains their own:
  - **Finance**: Separate cash, income, expenses
  - **Model**: Visual representation on the board
  - **PlayerItems**: Owned properties/businesses
  - **Position**: Current location on the board

## Customization

### Adjust AI Behavior
In `AIController`, you can modify:
- `purchaseProbability` (0-1): How likely AI is to purchase
- `decisionDelayMin/Max`: Time AI takes to make decisions
- Purchase logic in `MakePurchaseDecision()` method

### Change Number of Players
- **Before game starts**: Set `numberOfPlayers` in Inspector (1-4) or use `SetNumberOfPlayers()` method
- **During runtime**: Not recommended, but you can call `SetNumberOfPlayers()` and `InitializePlayers()` to reinitialize
- **Spawn Points**: Make sure you have at least as many spawn points as the number of players

### Player Names
Modify player names in `PlayerManager.InitializePlayers()`:
```csharp
string playerName = isAI ? $"AI Player {i}" : "Human Player";
```

## Testing

1. **Test Human Player**:
   - Verify you can roll dice and move
   - Verify purchases work correctly
   - Verify finance updates

2. **Test AI Players**:
   - Verify AI auto-rolls dice
   - Verify AI makes purchase decisions
   - Verify AI finance is separate

3. **Test Turn Switching**:
   - Verify turns cycle through all 4 players
   - Verify each player maintains separate data

## Troubleshooting

### Players Not Spawning
- Check PlayerManager has Player Prefab assigned
- Check spawn points are assigned
- Check Player prefab has required components

### AI Not Rolling
- Check AIController is initialized
- Check GameManager has PlayerManager reference
- Check `CanRollDice` conditions

### Finance Not Working
- Check PlayerManager.CurrentPlayer is not null
- Check PlayerFinance component exists on each player
- Check UI systems have PlayerManager reference

### Turn Not Switching
- Check `OnPlayerMovementComplete` is being called
- Check `SwitchToNextPlayer()` is being called
- Check PlayerManager events are subscribed

## Next Steps

1. **Visual Polish**:
   - Add player name displays
   - Add turn indicator UI
   - Add player finance displays for all players

2. **AI Improvements**:
   - Add different AI personalities (aggressive, conservative, etc.)
   - Add AI strategy variations
   - Add difficulty levels

3. **Game Features**:
   - Add win conditions
   - Add player elimination
   - Add game statistics

## Notes

- The system maintains backward compatibility with single-player code
- All player-specific operations now go through PlayerManager
- Each player's data is completely isolated
- AI players automatically handle their turns
- Human player controls remain the same (click/tap to roll)
