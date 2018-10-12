Param(
    [string]$instanceCount = "1",
    [string]$placementConstraint = "Primary==false"
)

# Define the scaling policy
function ScalingPolicies {
    $mechanism = New-Object -TypeName System.Fabric.Description.PartitionInstanceCountScaleMechanism
    $mechanism.MinInstanceCount = 1
    $mechanism.MaxInstanceCount = 2
    $mechanism.ScaleIncrement = 1
    $trigger = New-Object -TypeName System.Fabric.Description.AveragePartitionLoadScalingTrigger
    $trigger.MetricName = "polls"
    $trigger.LowerLoadThreshold = 2
    $trigger.UpperLoadThreshold = 5
    $trigger.ScaleInterval = New-TimeSpan -Seconds 10
    $scalingpolicy = New-Object -TypeName System.Fabric.Description.ScalingPolicyDescription
    $scalingpolicy.ScalingMechanism = $mechanism
    $scalingpolicy.ScalingTrigger = $trigger
    $scalingpolicies = New-Object 'System.Collections.Generic.List[System.Fabric.Description.ScalingPolicyDescription]'
    $scalingpolicies.Add($scalingpolicy)

    return $scalingpolicies
}

$serviceScalingpolicies = ScalingPolicies
# Define the metrics
$metric = @("polls,High,0,0")

# Check if service exists the service
$svc = Get-ServiceFabricService -ApplicationName "fabric:/Voting" -ServiceName "fabric:/Voting/VotingWeb"

<#
# Check if service exists
if ($svc)
{
    Update-ServiceFabricService -Stateless -ServiceName "fabric:/ScaleableWebAPI/WebAPI" `
    -InstanceCount $instanceCount -PlacementConstraints $placementConstraint -Metric $metric -ScalingPolicies $serviceScalingpolicies -Force
}
else
{
    New-ServiceFabricService -Stateless -ApplicationName "fabric:/ScaleableWebAPI" -ServiceName "fabric:/ScaleableWebAPI/WebAPI" -ServiceTypeName "WebAPIType" `
    -PartitionSchemeSingleton -InstanceCount $instanceCount -PlacementConstraint $placementConstraint -Metric $metric -ScalingPolicies $serviceScalingpolicies
}
#>

# Check if service exists local debug option...
if ($svc)
{
    Update-ServiceFabricService -Stateless -ServiceName "fabric:/Voting/VotingWeb" `
    -InstanceCount $instanceCount -Metric $metric -Force
}
else
{
    New-ServiceFabricService -Stateless -ApplicationName "fabric:/Voting" -ServiceName "fabric:/Voting/VotingWeb" -ServiceTypeName "VotingWebType" `
    -PartitionSchemeSingleton -InstanceCount $instanceCount -Metric $metric -ServicePackageActivationMode ExclusiveProcess
}