# Character Framework Tests

`NoUnity` contains the framework's standalone tests and source-contract checks.
They live outside `Assets` because `CharacterCommonModuleTests.cs` defines Unity-free
test doubles with the same type names as the runtime framework.

Run from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tests/CharacterFramework/NoUnity/run-tests.ps1
python Tests/CharacterFramework/NoUnity/verify_source_contracts.py
```
