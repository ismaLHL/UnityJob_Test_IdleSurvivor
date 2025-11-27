using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct FindClosestTargetJob : IJobParallelFor
{
	[ReadOnly] public NativeArray<float3> SeekersPosition;
	[ReadOnly] public NativeArray<float3> TargetsPosition;
	public NativeArray<float3> ClosestTargetPosition;
	public NativeArray<float3> ClosestTargetDirection;

	[BurstCompile]
	public void Execute(int index)
	{
		float lTargetDistance;
		float lClosestDistance = float.MaxValue;
		float3 lClosestTargetPosition = float3.zero;

		for (int i = TargetsPosition.Length - 1; i >= 0; i--)
		{
			lTargetDistance = math.distancesq(SeekersPosition[index], TargetsPosition[i]);
			if (lTargetDistance < lClosestDistance)
			{
				lClosestDistance = lTargetDistance;
				lClosestTargetPosition = TargetsPosition[i];
			}
		}

		ClosestTargetPosition[index] = lClosestTargetPosition;
		ClosestTargetDirection[index] = math.normalize(lClosestTargetPosition - SeekersPosition[index]);
	}
}
