param(
    [ValidateSet('Production', 'FeatureMissing')]
    [string]$TimerHandlerMode = 'Production',
    [ValidateSet('Production', 'FeatureMissing')]
    [string]$DamagePipelineMode = 'Production'
)

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$frameworkRoot = Join-Path $projectRoot 'Assets\00.Main\01.Script\Character\Framework'
$timerHandlePath = Join-Path $frameworkRoot 'Runtime\Core\CharacterTimerHandle.cs'
$movementHandlerPath = Join-Path $frameworkRoot 'Runtime\Modules\CharacterMovementHandler.cs'

$sourcePaths = @(
    (Join-Path $frameworkRoot 'Runtime\Core\CharacterActionType.cs'),
    (Join-Path $frameworkRoot 'Runtime\Core\CharacterDamageSource.cs'),
    (Join-Path $frameworkRoot 'Runtime\OwnedEntities\OwnedEntityPolicies.cs'),
    (Join-Path $frameworkRoot 'Runtime\Core\ProjectileDespawnReason.cs'),
    $timerHandlePath,
    (Join-Path $frameworkRoot 'Runtime\Modules\CharacterActionStateHandler.cs'),
    (Join-Path $frameworkRoot 'Runtime\Modules\CharacterCooldownHandler.cs'),
    (Join-Path $frameworkRoot 'Runtime\Modules\CharacterStatusHandler.cs'),
    (Join-Path $frameworkRoot 'Runtime\OwnedEntities\OwnedEntityDurabilityRules.cs'),
    $movementHandlerPath,
    (Join-Path $PSScriptRoot 'CharacterCommonModuleTests.cs')
)

if ($TimerHandlerMode -eq 'Production') {
    $sourcePaths += Join-Path $frameworkRoot 'Runtime\Modules\CharacterTimerHandler.cs'
}

if ($DamagePipelineMode -eq 'Production') {
    $sourcePaths += Join-Path $frameworkRoot 'Runtime\Modules\CharacterDamagePipeline.cs'
}

# This deliberately inert type lets the complete test suite compile without
# the production scheduler, so the FeatureMissing mode records a behavioral
# RED assertion rather than a compiler-only missing-type error.
$featureMissingTimerHandler = @'
namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterTimerHandler
    {
        public CharacterTimerHandle Schedule(float seconds, System.Action callback)
        {
            return new CharacterTimerHandle(1);
        }

        public bool Cancel(CharacterTimerHandle handle)
        {
            return false;
        }

        public void Tick(float deltaTime)
        {
        }

        public void CancelAll()
        {
        }
    }
}
'@

# This inert double permits a behavioral RED run for the damage pipeline.
$featureMissingDamagePipeline = @'
namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterDamagePipeline
    {
        public CharacterDamagePipeline(
            System.Func<float, CharacterDamageSource, float> modifyDamage,
            System.Action<float> requestDamage,
            System.Action<float> notifyDamageDealt)
        {
        }

        public void Apply(float amount, CharacterDamageSource source)
        {
        }
    }
}
'@

$usingDirectives = New-Object 'System.Collections.Generic.HashSet[string]'
$sources = foreach ($sourcePath in $sourcePaths) {
    if (Test-Path -LiteralPath $sourcePath) {
        $source = Get-Content -LiteralPath $sourcePath -Raw

        foreach ($match in [regex]::Matches($source, '(?m)^\s*using\s+[^;]+;\s*$')) {
            [void]$usingDirectives.Add($match.Value.Trim())
        }
        $source = [regex]::Replace($source, '(?m)^\s*using\s+[^;]+;\s*$', '')

        if ($sourcePath -eq $timerHandlePath) {
            if ($source -notmatch 'public\s+readonly\s+struct\s+CharacterTimerHandle') {
                Write-Error 'FAIL CharacterTimerHandle readonly contract'
                exit 1
            }

            # Windows PowerShell Add-Type uses an older C# compiler. Adapt only
            # this in-memory test input; the production source remains readonly.
            $source = $source -replace 'public readonly struct CharacterTimerHandle', 'public struct CharacterTimerHandle'
            $source = $source -replace 'internal CharacterTimerHandle\(int id\) => Id = id;', 'private readonly int id; internal CharacterTimerHandle(int id) { this.id = id; }'
            $source = $source -replace 'internal int Id \{ get; \}', 'internal int Id { get { return id; } }'
            $source = $source -replace 'public bool IsValid => Id > 0;', 'public bool IsValid { get { return id > 0; } }'
        }

        if ($sourcePath -eq $movementHandlerPath) {
            # Windows PowerShell Add-Type uses an older C# compiler.
            $source = $source -replace 'public bool MovementEnabled \{ get; private set; \} = true;', 'public bool MovementEnabled { get; private set; }'
            $source = $source -replace 'public float MovementSpeedMultiplier \{ get; private set; \} = 1f;', 'public float MovementSpeedMultiplier { get; private set; }'
            $source = $source -replace 'public int FacingDirection \{ get; private set; \} = 1;', 'public int FacingDirection { get; private set; }'
            $source = $source -replace 'Landed\?\.Invoke\(\);', 'if (Landed != null) Landed();'
            $source = $source -replace 'Jumped\?\.Invoke\(\);', 'if (Jumped != null) Jumped();'
            $source = [regex]::Replace(
                $source,
                '(this\.definition = definition;)',
                ('$1' + [Environment]::NewLine + '            MovementEnabled = true;' + [Environment]::NewLine + '            MovementSpeedMultiplier = 1f;' + [Environment]::NewLine + '            FacingDirection = 1;'))
        }

        $source.Trim()
    }
}

$typeDefinition = (($usingDirectives | Sort-Object) -join [Environment]::NewLine) +
    [Environment]::NewLine + [Environment]::NewLine +
    ($sources -join [Environment]::NewLine)
if ($TimerHandlerMode -eq 'FeatureMissing') {
    $typeDefinition += [Environment]::NewLine + $featureMissingTimerHandler
}
if ($DamagePipelineMode -eq 'FeatureMissing') {
    $typeDefinition += [Environment]::NewLine + $featureMissingDamagePipeline
}
$compiledTypes = Add-Type -TypeDefinition $typeDefinition -PassThru
$testType = $compiledTypes[0].Assembly.GetType('CharacterCommonModuleTests', $true)
$runMethod = $testType.GetMethod('Run', [Reflection.BindingFlags] 'Static, Public, NonPublic')
$failures = $runMethod.Invoke($null, @())

if ($failures -ne 0) {
    exit 1
}

Write-Output 'PASS CharacterCommonModuleTests'
