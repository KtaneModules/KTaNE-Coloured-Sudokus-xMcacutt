using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
	public Vector3 direction;
	public float speed;

	private void FixedUpdate()
	{
		transform.Rotate(direction, speed * Time.fixedDeltaTime);
	}
}
