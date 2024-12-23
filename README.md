# MiSideRichPresence
A mod for MiSide that adds support for RPC in-game, showing your current chapter and the time you've spent playing.

### [Скачать / Download](https://github.com/etakat/MiSideRichPresence/releases/latest/download/etakat.MiSideRichPresence.dll)

![image](https://github.com/user-attachments/assets/91e9bd36-5f1c-43b5-9079-d1d81fb097cc)
## Installation
1. Download the `.dll` from Releases
2. Put it inside `BepInEx/Plugins` folder
3. Launch the game.

## Building the mod
1. Grab `Il2Cppmscorlib.dll` and `UnityEngine.CoreModule.dll` from `BepInEx/interop` folder
2. Copy them over to Dependencies folder in this project
3. Run `dotnet restore` to download NuGet packages
4. Run `dotnet build -c Release` to build the project
5. Success!
   
## Credits
[Lachee](https://github.com/lachee) for making [`discord-rpc-csharp`](https://github.com/Lachee/discord-rpc-csharp), [`discord-rpc-unity`](https://github.com/Lachee/discord-rpc-unity) and [`unity-named-pipes`](https://github.com/Lachee/unity-named-pipes) libraries. 
