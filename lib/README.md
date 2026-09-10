# Game reference assemblies

The build looks here first. Drop the assemblies from

    <Valheim>\valheim_Data\Managed\

into this folder and the project compiles against the exact version you play. The files the
build needs are:

    assembly_valheim.dll
    assembly_utils.dll
    UnityEngine.dll
    UnityEngine.CoreModule.dll
    UnityEngine.IMGUIModule.dll
    UnityEngine.TextRenderingModule.dll
    UnityEngine.InputLegacyModule.dll
    UnityEngine.UIModule.dll
    UnityEngine.UI.dll
    UnityEngine.PhysicsModule.dll
    UnityEngine.AnimationModule.dll
    UnityEngine.AudioModule.dll
    UnityEngine.ParticleSystemModule.dll
    UnityEngine.ImageConversionModule.dll
    UnityEngine.AssetBundleModule.dll
    UnityEngine.InputModule.dll

If `assembly_valheim.dll` is absent the build fails with a message pointing here. There is no
NuGet fallback on purpose: the public `Valheim.GameLibs` package is pinned at 0.202.14, a Unity
2020 build of the game, and a mod compiled against it would not match what ships.

These are Iron Gate's files. `.gitignore` keeps `lib/*.dll` out of the repository - do not
commit or redistribute them.
