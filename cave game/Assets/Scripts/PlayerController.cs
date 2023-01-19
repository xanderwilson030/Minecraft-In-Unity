﻿using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	public float m_WalkSpeed = 6.0f;
	public float m_RunSpeed = 11.0f;
	public bool m_LimitDiagonalSpeed = true;
	public bool m_ToggleRun = false;
	public float m_JumpSpeed = 8.0f;
	public float m_Gravity = 20.0f;
	public float m_FallingThreshold = 10.0f;
	public bool m_SlideWhenOverSlopeLimit = false;
	public bool m_SlideOnTaggedObjects = false;
	public float m_SlideSpeed = 12.0f;
	public bool m_AirControl = false;
	public float m_AntiBumpFactor = .75f;
	public int m_AntiBunnyHopFactor = 1;
	public float mouseSens = 7;
	public Transform cam;

	float angle = 0;

	Vector3 m_MoveDirection = Vector3.zero;
	bool m_Grounded = false;
	CharacterController m_Controller;
	Transform m_Transform;
	float m_Speed;
	RaycastHit m_Hit;
	float m_FallStartLevel;
	bool m_Falling;
	float m_SlideLimit;
	float m_RayDistance;
	Vector3 m_ContactPoint;
	bool m_PlayerControl = false;
	int m_JumpTimer;

	public bool controlsEnabled = true;

	void Start()
	{
		m_Transform = GetComponent<Transform>();
		m_Controller = GetComponent<CharacterController>();

		m_Speed = m_WalkSpeed;
		m_RayDistance = m_Controller.height * .5f + m_Controller.radius;
		m_SlideLimit = m_Controller.slopeLimit - .1f;
		m_JumpTimer = m_AntiBunnyHopFactor;
	}

	public void Update()
	{
		if (PauseMenu.pauseMenu.paused || GetComponent<PlayerIO>().inventory.activeSelf)
		{
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
		}
		else
		{
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
			return;
		}
	}

	void Look()
	{
		if(!cam)
		{
			print("No camera assigned to PlayerController!");
			return;
		}

		float mouseX = Input.GetAxis("Mouse X") * mouseSens;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSens;

		mouseY = PauseMenu.pauseMenu.invertMouse ? -mouseY : mouseY;

		transform.eulerAngles += new Vector3(0, mouseX, 0);

		if(angle - mouseY > 90)
		{
			angle = 90;
		}
		else if(angle - mouseY < -90)
		{
			angle = -90;
		}
		else
		{
			angle -= mouseY;
		}

		cam.localEulerAngles = new Vector3(angle, 0, 0);
	}

	void FixedUpdate()
	{
		if (PauseMenu.pauseMenu.paused || GetComponent<PlayerIO>().inventory.activeSelf || !controlsEnabled) return;

		float inputX = Input.GetAxis("Horizontal");
		float inputY = Input.GetAxis("Vertical");

		float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f && m_LimitDiagonalSpeed) ? .7071f : 1.0f;

		if (m_Grounded)
		{
			bool sliding = false;
			if (Physics.Raycast(m_Transform.position, -Vector3.up, out m_Hit, m_RayDistance))
			{
				if (Vector3.Angle(m_Hit.normal, Vector3.up) > m_SlideLimit)
				{
					sliding = true;
				}
			}
			else
			{
				Physics.Raycast(m_ContactPoint + Vector3.up, -Vector3.up, out m_Hit);
				if (Vector3.Angle(m_Hit.normal, Vector3.up) > m_SlideLimit)
				{
					sliding = true;
				}
			}
			if (m_Falling)
			{
				m_Falling = false;
				if (m_Transform.position.y < m_FallStartLevel - m_FallingThreshold)
				{
					OnFell(m_FallStartLevel - m_Transform.position.y);
				}
			}
			if (!m_ToggleRun)
			{
				m_Speed = Input.GetKey(KeyCode.LeftShift) ? m_RunSpeed : m_WalkSpeed;
			}
			if ((sliding && m_SlideWhenOverSlopeLimit) || (m_SlideOnTaggedObjects && m_Hit.collider.tag == "Slide"))
			{
				Vector3 hitNormal = m_Hit.normal;
				m_MoveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
				Vector3.OrthoNormalize(ref hitNormal, ref m_MoveDirection);
				m_MoveDirection *= m_SlideSpeed;
				m_PlayerControl = false;
			}
			else
			{
				m_MoveDirection = new Vector3(inputX * inputModifyFactor, -m_AntiBumpFactor, inputY * inputModifyFactor);
				m_MoveDirection = m_Transform.TransformDirection(m_MoveDirection) * m_Speed;
				m_PlayerControl = true;
			}
			if (!Input.GetButton("Jump"))
			{
				m_JumpTimer++;
			}
			else if (m_JumpTimer >= m_AntiBunnyHopFactor)
			{
				m_MoveDirection.y = m_JumpSpeed;
				m_JumpTimer = 0;
			}
		}
		else
		{
			if (!m_Falling)
			{
				m_Falling = true;
				m_FallStartLevel = m_Transform.position.y;
			}
			if (m_AirControl && m_PlayerControl)
			{
				m_MoveDirection.x = inputX * m_Speed * inputModifyFactor;
				m_MoveDirection.z = inputY * m_Speed * inputModifyFactor;
				m_MoveDirection = m_Transform.TransformDirection(m_MoveDirection);
			}
		}

		m_MoveDirection.y -= m_Gravity * Time.deltaTime;
		m_Grounded = (m_Controller.Move(m_MoveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;

		Look();
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		m_ContactPoint = hit.point;
	}

	void OnFell(float fallDistance)
	{
		if (fallDistance >= 4 && fallDistance < 12)
			SoundManager.PlayAudio("fallsmall", 0.25f, Random.Range(0.9f, 1.1f));
		else if (fallDistance >= 12)
		{
			SoundManager.PlayAudio("fallbig", 0.25f, Random.Range(0.9f, 1.1f));
			World.currentWorld.SpawnLandParticles();
		}
	}
}