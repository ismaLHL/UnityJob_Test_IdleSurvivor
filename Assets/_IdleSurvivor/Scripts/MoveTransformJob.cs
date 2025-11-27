using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public struct MoveTransformJob : IJobParallelForTransform
{
	public NativeArray<float3> velocity;
	public float deltaTime;

	[BurstCompile]
	public void Execute(int index, TransformAccess transform)
	{
		float3 currentPosition = transform.position;
		float3 move = velocity[index] * deltaTime;
		transform.position = currentPosition + move;
	}
}
