using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;


public struct TransformInfo
{
	public int ID;
	public Transform Transform;
	public Vector3 Velocity;
	public float Radius;
}

public class LevelManager : MonoBehaviour
{
	[SerializeField] private Transform Player;
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private float _projectileRadius = 0.75f;
	[SerializeField] private float _projectileSpeed = 10f;
	[SerializeField] private float _projectileSpawnRate = 2f;
	[SerializeField] private GameObject _enemyPrefab;
	[SerializeField] private float _enemyRadius = 0.75f;
	[SerializeField] private Vector2 _speedRange = new Vector2(1f, 3f);
	[SerializeField] private float _enemiesSpawnRate = 5f;
	[SerializeField] private float _spawnRadius = 10f;

	static public LevelManager Instance { get; private set; }

	private List<TransformInfo> _projectiles = new List<TransformInfo>();
	private List<TransformInfo> _enemies = new List<TransformInfo>();
	private List<TransformInfo> _transformsToMove = new List<TransformInfo>();

	private float _enemiesSpawnTimer = 0f;
	private float _projectileSpawnTimer = 0f;
	private int _nextTransformID = 0;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		_enemiesSpawnTimer = 1.0f;
		_projectileSpawnTimer = 1.0f;
	}

	// Update is called once per frame
	void Update()
	{
		_enemiesSpawnTimer -= Time.deltaTime * _enemiesSpawnRate;

		while (_enemiesSpawnTimer <= 0.0f)
		{
			SpawnEnemy();
			_enemiesSpawnTimer += 1.0f;
		}

		_projectileSpawnTimer -= Time.deltaTime * _projectileSpawnRate;

		while (_projectileSpawnTimer <= 0.0f)
		{
			_projectileSpawnTimer += 1.0f;

			Vector3 lDirectionToClosestEnemy;
			FindDirectionToClosestEnemy(Vector3.zero, out lDirectionToClosestEnemy);
			SpawnProjectile(lDirectionToClosestEnemy);

			Player.rotation = Quaternion.LookRotation(lDirectionToClosestEnemy, Vector3.up);
		}

		MoveTransforms();
		//TestCollisions();
	}

	private void TestCollisions()
	{
		throw new NotImplementedException();
	}

	private void FindDirectionToClosestEnemy(Vector3 pSeekerPosition, out Vector3 ClosestEnemyDirection)
	{
		NativeArray<float3> lEnemiesPostion = new NativeArray<float3>(_enemies.Count, Allocator.TempJob);

		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			lEnemiesPostion[i] = _enemies[i].Transform.position;
		}

		NativeArray<float3> lSeekerPositionArray = new NativeArray<float3>(1, Allocator.TempJob);
		lSeekerPositionArray[0] = (float3)pSeekerPosition;
		NativeArray<float3> lClosestTargetDirectionArray = new NativeArray<float3>(1, Allocator.TempJob);
 		NativeArray<float3> lClosestTargetPostionArray = new NativeArray<float3>(1, Allocator.TempJob);

		FindClosestTargetJob lJob = new FindClosestTargetJob
		{
			SeekersPosition = lSeekerPositionArray,
			TargetsPosition = lEnemiesPostion,
			ClosestTargetDirection = lClosestTargetDirectionArray,
			ClosestTargetPosition = lClosestTargetPostionArray
		};

		JobHandle lJobHandle = lJob.Schedule(1, 1);
		lJobHandle.Complete();

		ClosestEnemyDirection = lClosestTargetDirectionArray[0];
	}

	private JobHandle MoveTransforms(JobHandle pDependencies = default)
	{
		// Prepare the data for the job

		// Create TransformAccessArray from enemy transforms
		Transform[] lTransforms = new Transform[_transformsToMove.Count];
		for (int i = _transformsToMove.Count - 1; i >= 0; i--)
		{
			lTransforms[i] = _transformsToMove[i].Transform;
		}

		TransformAccessArray lTransformAccessArray = new UnityEngine.Jobs.TransformAccessArray(lTransforms);

		// Create NativeArray for velocities
		NativeArray<float3> lVelocities = new NativeArray<float3>(_transformsToMove.Count, Allocator.TempJob);
		for (int i = _transformsToMove.Count - 1; i >= 0; i--)
		{
			lVelocities[i] = _transformsToMove[i].Velocity;
		}

		MoveTransformJob lMoveJob = new MoveTransformJob
		{
			velocity = lVelocities,
			deltaTime = Time.deltaTime,
		};

		JobHandle lJobHandle = lMoveJob.Schedule(lTransformAccessArray, pDependencies);
		lJobHandle.Complete();

		lVelocities.Dispose();
		lTransformAccessArray.Dispose();

		return lJobHandle;
	}

	public void AddTransformToMove(TransformInfo pTransformInfo)
	{
		_transformsToMove.Add(pTransformInfo);
	}

	private void SpawnProjectile(Vector3 Direction)
	{
		Vector3 lSpawnPosition = Vector3.zero;
		Quaternion lRotation = Quaternion.LookRotation(Vector3.forward, Direction);
		
		GameObject lProjectileGO = Instantiate(_projectilePrefab, lSpawnPosition, lRotation);
		TransformInfo lProjectileInfo = new TransformInfo
		{
			ID = _nextTransformID++,
			Transform = lProjectileGO.transform,
			Velocity = Direction.normalized * _projectileSpeed,
			Radius = _projectileRadius,
		};
		_projectiles.Add(lProjectileInfo);
		_transformsToMove.Add(lProjectileInfo);
	}

	private void SpawnEnemy()
	{
		Vector3 lRandomUnitVector = UnityEngine.Random.onUnitSphere;
		Vector3 lSpawnPosition = lRandomUnitVector * _spawnRadius;
		Quaternion lRotation = Quaternion.LookRotation(-lRandomUnitVector, Vector3.up);
		float lSpeed = UnityEngine.Random.Range(_speedRange.x, _speedRange.y);

		GameObject lEnemyGO = Instantiate(_enemyPrefab, lSpawnPosition, lRotation);

		TransformInfo lEnemyInfo = new TransformInfo
		{
			ID = _nextTransformID++,
			Transform = lEnemyGO.transform,
			Velocity = -lRandomUnitVector * lSpeed,
			Radius = _enemyRadius,
		};

		_enemies.Add(lEnemyInfo);
		_transformsToMove.Add(lEnemyInfo);
	}

	public void DeleteEnemy(Transform transform)
	{
		// find and remove from enemies
		_enemies.RemoveAll(e => e.Transform == transform);
		// find and remove from transforms to move
		_transformsToMove.RemoveAll(t => t.Transform == transform);

		Destroy(transform.gameObject);
	}

	public void DeleteProjectile(Transform transform)
	{
		// find and remove from projectiles
		_projectiles.RemoveAll(p => p.Transform == transform);
		// find and remove from transforms to move
		_transformsToMove.RemoveAll(t => t.Transform == transform);

		Destroy(transform.gameObject);
	}
}
