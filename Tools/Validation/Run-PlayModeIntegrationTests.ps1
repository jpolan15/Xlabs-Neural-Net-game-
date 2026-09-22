<#
.SYNOPSIS
    Compiles and executes PlayMode integration tests under Mono runtime for Gameplay & Level 1 systems.
#>

$ErrorActionPreference = "Stop"

$workspace = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tempDir = Join-Path $workspace "Temp"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
}

$monoExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\MonoBleedingEdge\bin\mono.exe"
$cscExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe"
$nunitDll = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ext.nunit\net472\unity-custom\nunit.framework.dll"
$netStandardFacade = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\MonoBleedingEdge\lib\mono\4.7.2-api\Facades\netstandard.dll"
$unityManaged = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed\UnityEngine"
$unityCoreDll = Join-Path $unityManaged "UnityEngine.CoreModule.dll"
$unityEngineDll = Join-Path $unityManaged "UnityEngine.dll"
$unityPhysicsDll = Join-Path $unityManaged "UnityEngine.PhysicsModule.dll"

Write-Host "Compiling Core & Gameplay assemblies..." -ForegroundColor Cyan

# 1. Core.Math
$mathOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Math.dll")
$mathSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Math\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathOut $mathSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Math" }

# 2. Core.Neural
$neuralOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Neural.dll")
$mathRef = "/reference:" + (Join-Path $tempDir "Convergence.Core.Math.dll")
$neuralSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Neural\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathRef $neuralOut $neuralSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Neural" }

# 3. Core.Training
$trainingOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Training.dll")
$trainingSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Training\*.cs")).FullName
& $monoExe $cscExe "/target:library" $trainingOut $trainingSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Training" }

# 4. Core.Puzzles
$puzzlesOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Puzzles.dll")
$neuralRef = "/reference:" + (Join-Path $tempDir "Convergence.Core.Neural.dll")
$puzzlesSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Puzzles\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathRef $neuralRef $puzzlesOut $puzzlesSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Puzzles" }

# 5. Convergence.Gameplay
$gameplayOut = "/out:" + (Join-Path $tempDir "Convergence.Gameplay.dll")
$puzzlesRef = "/reference:" + (Join-Path $tempDir "Convergence.Core.Puzzles.dll")
$gameplaySrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Gameplay\*.cs")).FullName
& $monoExe $cscExe "/target:library" "/reference:$netStandardFacade" $mathRef $neuralRef $puzzlesRef "/reference:$unityCoreDll" "/reference:$unityEngineDll" "/reference:$unityPhysicsDll" $gameplayOut $gameplaySrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Convergence.Gameplay" }

# 6. PlayMode Tests
$playmodeOut = "/out:" + (Join-Path $tempDir "Convergence.Tests.PlayMode.dll")
$gameplayRef = "/reference:" + (Join-Path $tempDir "Convergence.Gameplay.dll")
$nunitRef = "/reference:$nunitDll"
$playmodeSrc = (Get-ChildItem (Join-Path $workspace "Tests\PlayMode\Gameplay\*.cs")).FullName
& $monoExe $cscExe "/target:library" "/reference:$netStandardFacade" $mathRef $neuralRef $puzzlesRef $gameplayRef "/reference:$unityCoreDll" "/reference:$unityEngineDll" $nunitRef $playmodeOut $playmodeSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for PlayMode tests" }

# Copy runtime dependencies into Temp
Copy-Item $nunitDll $tempDir -Force
Get-ChildItem $unityManaged -Filter "*.dll" | Copy-Item -Destination $tempDir -Force


# 7. Generate Standalone Mono Test Runner Entrypoint
$runnerSourcePath = Join-Path $tempDir "PlayModeRunnerEntry.cs"
$runnerExePath = Join-Path $tempDir "PlayModeRunnerEntry.exe"
$playmodeDllPath = (Join-Path $tempDir "Convergence.Tests.PlayMode.dll").Replace('\', '/')

$runnerCode = @"
using System;
using System.Reflection;

namespace Convergence.TestRunner
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Executing PlayMode Integration Tests under Mono CLR...");
            int passed = 0;
            int failed = 0;

            Assembly testAsm = Assembly.LoadFrom("$playmodeDllPath");

            foreach (Type type in testAsm.GetTypes())
            {
                bool isFixture = false;
                foreach (var attr in type.GetCustomAttributes(true))
                {
                    if (attr.GetType().Name == "TestFixtureAttribute") { isFixture = true; break; }
                }

                if (isFixture)
                {
                    object instance = Activator.CreateInstance(type);
                    MethodInfo setupMethod = null;
                    MethodInfo teardownMethod = null;

                    foreach (var m in type.GetMethods())
                    {
                        foreach (var attr in m.GetCustomAttributes(true))
                        {
                            if (attr.GetType().Name == "SetUpAttribute") setupMethod = m;
                            if (attr.GetType().Name == "TearDownAttribute") teardownMethod = m;
                        }
                    }

                    foreach (var method in type.GetMethods())
                    {
                        bool isTest = false;
                        foreach (var attr in method.GetCustomAttributes(true))
                        {
                            if (attr.GetType().Name == "TestAttribute") { isTest = true; break; }
                        }

                        if (isTest)
                        {
                            try
                            {
                                if (setupMethod != null) setupMethod.Invoke(instance, null);
                                method.Invoke(instance, null);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("  PASS: " + type.Name + "." + method.Name);
                                Console.ResetColor();
                                passed++;
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                                Console.WriteLine("  FAIL: " + type.Name + "." + method.Name + " -> " + msg);
                                Console.ResetColor();
                                failed++;
                            }
                            finally
                            {
                                if (teardownMethod != null)
                                {
                                    try { teardownMethod.Invoke(instance, null); } catch {}
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            if (failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"PlayMode Test Results: Total {passed + failed} | Passed: {passed} | Failed: {failed}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"PlayMode Test Results: Total {passed + failed} | Passed: {passed} | Failed: {failed}");
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.ResetColor();

            return failed > 0 ? 1 : 0;
        }
    }
}
"@

Set-Content -Path $runnerSourcePath -Value $runnerCode -Encoding UTF8

$runnerRef = "/reference:" + (Join-Path $tempDir "Convergence.Tests.PlayMode.dll")
& $monoExe $cscExe "/target:exe" "/reference:$netStandardFacade" $mathRef $neuralRef $puzzlesRef $gameplayRef "/reference:$unityCoreDll" "/reference:$unityEngineDll" $nunitRef $runnerRef ("/out:" + $runnerExePath) $runnerSourcePath
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for PlayMode runner" }

# Execute the runner under Mono
& $monoExe $runnerExePath
exit $LASTEXITCODE
